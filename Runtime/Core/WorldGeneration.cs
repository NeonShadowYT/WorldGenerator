using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;
using System.Threading;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Stopwatch = System.Diagnostics.Stopwatch;

namespace NeonImperium.WorldGenerations
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class WorldGeneration : MonoBehaviour
    {
        private static int _globalSeedCounter;
        private static readonly Stopwatch UptimeStopwatch = Stopwatch.StartNew();
        private const int PlacementAttemptsPerFrame = 25;

        [SerializeField]
        [Tooltip("Seed генерации. 0 = уникальный при каждом запуске (если useDeterministicSeed = false).")]
        private int _seed;

        public int Seed
        {
            get => _seed;
            set { _seed = value; _modulesInitialized = false; }
        }

        public SpawnSettings settings = new();

        public int SpawnedCount => transform.childCount;
        public int ValidPlacementCount => _placementCache.Count;
        public bool IsGenerating => _generationInProgress;
        public int TotalPlacementAttempts => _totalPlacementAttempts;

        public event Action OnGenerationComplete;

        [System.NonSerialized] private List<PlacementData> _placementCache = new();
        [System.NonSerialized] private List<PlacementData> _previewCache = new();
        [System.NonSerialized] private readonly List<PlacementData> _previewValid = new();
        [SerializeField, HideInInspector] private int _totalPlacementAttempts;

        public Dictionary<FailureReasonType, int> FailureStatistics { get; private set; } = new();
        public IReadOnlyList<PlacementData> PreviewCache => _previewCache;

        private IPointGenerator _pointGenerator;
        private IPlacementValidator _placementValidator;
        private IPhysicsService _physicsService;
        private IDebugLogger _debugLogger = NullDebugLogger.Instance;
        private SpatialHash _placementHash;

        [System.NonSerialized] private PlacementData[] _batchOutput;
        [System.NonSerialized] private readonly List<PlacementPoint> _batchCandidates = new(128);
        private List<NavMeshBuildSource> _runtimeNavMeshSources;

        private bool _generationInProgress;
        private bool _editorInstantiationActive;
        private int _generationVersion;
        private int _runtimeRetryCount;
        private bool _modulesInitialized;

        private CancellationTokenSource _generationCts;

        public List<DebugRay> debugRays => _debugLogger?.GetDebugRays() ?? new List<DebugRay>();
        public void ClearDebugRays() => _debugLogger?.ClearDebugRays();
        public int GetClusterCentersCount() => _pointGenerator is ClusterPointGenerator cpg ? cpg.GetClusterCount() : 0;

        public void AddDebugRay(Vector3 start, Vector3 direction, float distance, Color color, DebugRayType rayType)
            => _debugLogger.TryAddRay(start, direction, distance, color, rayType);

        private bool CanUseJobValidation =>
            settings.useJobValidation
            && settings.rayCastType == RayCastType.Ray
            && _placementValidator is PlacementValidator { JobsAvailable: true };

        private int GetFrameBatchSize() =>
            CanUseJobValidation ? Mathf.Max(PlacementAttemptsPerFrame, settings.jobBatchSize) : PlacementAttemptsPerFrame;

        private void Awake()
        {
            if (!Application.isPlaying) return;
            if (!settings.enableRuntimeGeneration) { Destroy(this); return; }
            EnsureModulesInitialized();
            if (settings.autoGenerateOnStart) RuntimeSpawnQueueService.Enqueue(this);
        }

        private void OnDestroy()
        {
            StopGeneration();
            _placementValidator?.Dispose();
            _placementValidator = null;
            _generationCts?.Dispose();
            _generationCts = null;
        }

        private void OnValidate()
        {
            _previewCache.Clear();
            _previewValid.Clear();
            settings?.Clamp();

#if UNITY_EDITOR
            if (!Application.isPlaying)
                StopGeneration();
#endif

            _modulesInitialized = false;
        }

        private void EnsureModulesInitialized()
        {
            if (_modulesInitialized) return;

            _placementValidator?.Dispose();
            _placementValidator = null;

            _physicsService ??= new PhysicsService();
            _debugLogger = new DebugLogger(settings.debugRaySettings);

            int resolved = SpawnSettings.ResolveSeed(_seed, settings.useDeterministicSeed);
            int seed = resolved != 0 ? resolved : GetUniqueSeed();

            List<ISpawnZone> zones = SpawnZoneBuilder.Build(settings);
            _placementValidator = new PlacementValidator(settings, seed, zones);

            _pointGenerator = settings.useClustering ? new ClusterPointGenerator() : new RandomPointGenerator();
            _pointGenerator.Initialize(settings, transform, _debugLogger, _physicsService, seed, zones);

            _placementHash = settings.minDistanceBetweenObjects > 0
                ? new SpatialHash(settings.minDistanceBetweenObjects, Mathf.Clamp(settings.population, 16, 8192))
                : null;

            int frameBatch = GetFrameBatchSize();
            if (_batchOutput == null || _batchOutput.Length < frameBatch)
                _batchOutput = new PlacementData[Mathf.NextPowerOfTwo(Mathf.Max(frameBatch, 64))];

            if (_batchCandidates.Capacity < frameBatch)
                _batchCandidates.Capacity = frameBatch;

            _modulesInitialized = true;
        }

        private static int GetUniqueSeed()
        {
            long ticks = UptimeStopwatch.ElapsedTicks;
            int tickHash = unchecked((int)(ticks ^ (ticks >> 32)));
            int counter = Interlocked.Increment(ref _globalSeedCounter);
            return tickHash + Environment.TickCount + counter;
        }

        [ContextMenu("Generate Objects")]
        public void Generate()
        {
            // Отбиваем только если действительно идут обе фазы.
            // Если застрял один флаг от прошлой отмены — начинаем заново (старую async
            // инвалидирует version counter).
            if (_generationInProgress && _editorInstantiationActive)
            {
                UnityEngine.Debug.LogWarning($"[{name}] Генерация уже выполняется, повторный запуск проигнорирован.");
                return;
            }

            if (settings.prefabs == null || settings.prefabs.Length == 0)
            {
                UnityEngine.Debug.LogError("Префаб не настроен!", this);
                return;
            }
            if (settings.collisionMask.value == 0)
            {
                UnityEngine.Debug.LogError("Collision Mask не настроен!", this);
                return;
            }

            if (settings.reuseExistingObjects) ClearGenerationState();
            else ClearAll();

            EnsureModulesInitialized();

            _generationCts?.Dispose();
            _generationCts = new CancellationTokenSource();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                _generationInProgress = true;
                _editorInstantiationActive = false;
                EditorGenerationDriver.Start(this);
                UnityEngine.Debug.Log($"[{name}] Запущена генерация в редакторе (асинхронно).");
                return;
            }
#endif
            if (Application.isPlaying) _ = GenerateAsync();
        }

        public void GeneratePreview()
        {
            _previewCache.Clear();
            _previewValid.Clear();
            if (settings.prefabs == null || settings.prefabs.Length == 0) return;
            if (settings.collisionMask.value == 0) return;

            EnsureModulesInitialized();
            if (_pointGenerator == null || _placementValidator == null) return;

            int attempts = Mathf.Clamp(settings.previewAttempts, 10, 1000);
            for (int i = 0; i < attempts; i++)
            {
                PlacementPoint point = _pointGenerator.GeneratePoint();
                if (point.zoneIndex < 0)
                {
                    _previewCache.Add(new PlacementData { isValid = false, failureReason = FailureReasonType.NoHit });
                    continue;
                }

                PlacementData data = _placementValidator.Validate(point, transform, _previewValid,
                    _debugLogger, _physicsService, null);
                _previewCache.Add(data);
                if (data.isValid) _previewValid.Add(data);
            }
        }

        public void ClearPreview()
        {
            _previewCache.Clear();
            _previewValid.Clear();
        }

        public async Awaitable GenerateAsync()
        {
            if (_generationInProgress) return;
            if (settings.prefabs == null || settings.prefabs.Length == 0)
            {
                UnityEngine.Debug.LogError("Префаб не настроен!", this);
                return;
            }
            if (settings.collisionMask.value == 0)
            {
                UnityEngine.Debug.LogError("Collision Mask не настроен!", this);
                return;
            }

            if (settings.reuseExistingObjects) ClearGenerationState();
            else ClearAll();

            EnsureModulesInitialized();

            _generationCts?.Dispose();
            _generationCts = new CancellationTokenSource();
            CancellationToken token = _generationCts.Token;
            int version = ++_generationVersion;

            _generationInProgress = true;
            _runtimeRetryCount = 0;
            bool completed = false;

            try
            {
                while (!completed && _runtimeRetryCount < settings.runtimeRetryCount + 1)
                {
                    token.ThrowIfCancellationRequested();

                    _placementCache.Clear();
                    _totalPlacementAttempts = 0;
                    FailureStatistics.Clear();
                    ClearDebugRays();
                    _placementHash?.Clear();
                    _batchCandidates.Clear();

                    if (_runtimeRetryCount > 0)
                    {
                        _modulesInitialized = false;
                        EnsureModulesInitialized();
                    }

                    while (_placementCache.Count < settings.population &&
                           (_pointGenerator.HasMorePoints() || !settings.useClustering))
                    {
                        token.ThrowIfCancellationRequested();

                        if (_totalPlacementAttempts >= settings.runtimeMaxAttempts)
                        {
                            UnityEngine.Debug.LogWarning($"[{name}] Достигнут лимит попыток.");
                            break;
                        }

                        int batch = Mathf.Min(GetFrameBatchSize(), settings.population - _placementCache.Count);
                        GeneratePlacementPointsBatch(batch);

                        if (_placementCache.Count < settings.population)
                            await Awaitable.NextFrameAsync(token);
                    }

                    if (_placementCache.Count >= settings.population) { completed = true; break; }
                    _runtimeRetryCount++;
                    if (_runtimeRetryCount >= settings.runtimeRetryCount + 1) completed = true;
                }

                await CreateObjectsFromPlacementData(token);

                if (version != _generationVersion || token.IsCancellationRequested) return;

                await TryRuntimeNavMeshRegenerationAsync(token);

                if (version != _generationVersion || token.IsCancellationRequested) return;

                CallGenerationCompleteExtensions();
                OnGenerationComplete?.Invoke();

                if (settings.destroyAfterGeneration && Application.isPlaying)
                    Destroy(this);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e, this);
            }
            finally
            {
                if (version == _generationVersion)
                    _generationInProgress = false;
            }
        }

        private void GeneratePlacementPointsBatch(int desiredCount)
        {
            if (_pointGenerator == null || _placementValidator == null) return;
            ClearDebugRays();

            if (CanUseJobValidation) { GeneratePlacementPointsBatchJob(desiredCount); return; }

            int validPoints = 0, attempts = 0;
            int maxAttempts = desiredCount * settings.maxPlacementAttempts;

            while (validPoints < desiredCount && attempts++ < maxAttempts)
            {
                if (_placementCache.Count >= settings.population) break;
                _totalPlacementAttempts++;

                PlacementPoint point = _pointGenerator.GeneratePoint();
                if (point.zoneIndex < 0) continue;

                PlacementData data = _placementValidator.Validate(point, transform, _placementCache,
                    _debugLogger, _physicsService, _placementHash);

                if (data.isValid)
                {
                    _placementCache.Add(data);
                    _placementHash?.Insert(data.position, _placementCache.Count - 1);
                    validPoints++;
                }
                else RecordFailure(data.failureReason);
            }
        }

        private void GeneratePlacementPointsBatchJob(int desiredCount)
        {
            int wanted = desiredCount;
            int remaining = settings.population - _placementCache.Count;
            if (wanted > remaining) wanted = remaining;
            if (wanted <= 0) return;

            _batchCandidates.Clear();

            int maxAttempts = wanted * settings.maxPlacementAttempts;
            int attempts = 0;

            while (_batchCandidates.Count < wanted && attempts++ < maxAttempts)
            {
                _totalPlacementAttempts++;
                PlacementPoint point = _pointGenerator.GeneratePoint();
                if (point.zoneIndex < 0) continue;
                _batchCandidates.Add(point);
            }

            int count = _batchCandidates.Count;
            if (count <= 0) return;

            if (_batchOutput == null || _batchOutput.Length < count)
                _batchOutput = new PlacementData[Mathf.NextPowerOfTwo(count)];

            _placementValidator.ValidateBatch(_batchCandidates, 0, count, transform,
                _debugLogger, _physicsService, _placementHash, _batchOutput);

            float minDist = settings.minDistanceBetweenObjects;
            bool useMinDist = _placementHash != null && minDist > 0f;

            for (int i = 0; i < count; i++)
            {
                PlacementData data = _batchOutput[i];
                if (!data.isValid)
                {
                    RecordFailure(data.failureReason);
                    continue;
                }

                if (useMinDist && _placementHash.HasWithin(data.position, minDist))
                {
                    RecordFailure(FailureReasonType.TooCloseToOther);
                    continue;
                }

                _placementCache.Add(data);
                _placementHash?.Insert(data.position, _placementCache.Count - 1);
            }
        }

        private void RecordFailure(FailureReasonType reason)
        {
            FailureStatistics.TryGetValue(reason, out int count);
            FailureStatistics[reason] = count + 1;
        }

        private async Awaitable CreateObjectsFromPlacementData(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            bool wasActive = gameObject.activeSelf;
            bool deactivated = false;

            try
            {
                if (Application.isPlaying)
                {
                    gameObject.SetActive(false);
                    deactivated = true;
                    await Awaitable.NextFrameAsync(token);
                }

                await ObjectInstantiator.InstantiateBatchAsync(
                    _placementCache, transform, settings.population,
                    settings.onSpawnExtensions, settings.reuseExistingObjects,
                    settings.prefabs, token);
            }
            finally
            {
                if (deactivated) gameObject.SetActive(wasActive);
            }
        }

        private async Awaitable TryRuntimeNavMeshRegenerationAsync(CancellationToken token)
        {
            if (!Application.isPlaying) return;
            if (!settings.requestNavMeshRegeneration) return;

            if (NavMeshRegenerationService.HasNavMeshSurface())
            {
                int rebuilt = NavMeshRegenerationService.RebuildAllSurfacesSync();
                if (rebuilt > 0) return;
            }

            NavMeshData data = settings.runtimeNavMeshData;
            if (data == null) return;

            if (_runtimeNavMeshSources == null)
                _runtimeNavMeshSources = new List<NavMeshBuildSource>(256);

            Bounds bounds = GetRuntimeBounds();

            AsyncOperation op = RuntimeNavMeshBuilder.RegenerateAsync(
                data, bounds, NavMeshCollectGeometry.PhysicsColliders,
                settings.collisionMask.value, settings.runtimeAgentTypeID, _runtimeNavMeshSources);

            if (op == null) return;

            while (!op.isDone)
            {
                token.ThrowIfCancellationRequested();
                await Awaitable.NextFrameAsync(token);
            }
        }

        private Bounds GetRuntimeBounds()
        {
            Bounds b = settings.runtimeBounds;
            if (b.size.sqrMagnitude < 1e-4f)
                return new Bounds(transform.position, settings.dimensions * 2f);
            return new Bounds(transform.TransformPoint(b.center), b.size);
        }

        private void CallGenerationCompleteExtensions()
        {
            for (int i = 0; i < settings.onGenerationCompleteExtensions.Count; i++)
            {
                WorldGenerationExtension e = settings.onGenerationCompleteExtensions[i];
                if (e != null) e.OnGenerationComplete();
            }
        }

        [ContextMenu("Clear Objects")]
        public void ClearAll()
        {
            StopGeneration();
            while (transform.childCount > 0) DestroyImmediate(transform.GetChild(0).gameObject);
            ResetGenerationState();
        }

        private void ClearGenerationState()
        {
            StopGeneration();
            ResetGenerationState();
        }

        private void ResetGenerationState()
        {
            _placementCache.Clear();
            _previewCache.Clear();
            _previewValid.Clear();
            _totalPlacementAttempts = 0;
            _runtimeRetryCount = 0;
            ClearDebugRays();
            FailureStatistics.Clear();
            _placementHash?.Clear();
            _batchCandidates.Clear();
            _modulesInitialized = false;
        }

        private void StopGeneration()
        {
            _generationVersion++;      // инвалидирует любую висящую async
            _generationCts?.Cancel();

#if UNITY_EDITOR
            EditorGenerationDriver.Stop(this);
#endif

            _generationInProgress = false;
            _editorInstantiationActive = false; // форсируем разблокировку
        }

#if UNITY_EDITOR
        internal void EditorTick()
        {
            if (!_generationInProgress) return;
            EnsureModulesInitialized();

            if (_pointGenerator == null || _placementValidator == null)
            {
                StopGeneration();
                return;
            }

            if (_placementCache.Count < settings.population)
            {
                if (_totalPlacementAttempts >= settings.runtimeMaxAttempts)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[{name}] Достигнут лимит попыток ({_totalPlacementAttempts}). " +
                        $"Размещено {_placementCache.Count}/{settings.population}. Завершаю.");

                    if (!_editorInstantiationActive)
                    {
                        _editorInstantiationActive = true;
                        int v = _generationVersion;
                        _ = FinishEditorGenerationAsync(v);
                    }
                    return;
                }

                GeneratePlacementPointsBatch(GetFrameBatchSize());
            }
            else if (!_editorInstantiationActive)
            {
                _editorInstantiationActive = true;
                int v = _generationVersion;
                _ = FinishEditorGenerationAsync(v);
            }
        }

        private async Awaitable FinishEditorGenerationAsync(int version)
        {
            CancellationToken token = _generationCts?.Token ?? CancellationToken.None;
            try
            {
                await CreateObjectsFromPlacementData(token);

                if (version != _generationVersion || token.IsCancellationRequested) return;

                CallGenerationCompleteExtensions();
                OnGenerationComplete?.Invoke();
                UnityEngine.Debug.Log($"[{name}] Генерация завершена. Создано: {transform.childCount}");
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e, this);
            }
            finally
            {
                if (version == _generationVersion)
                {
                    _editorInstantiationActive = false;
                    _generationInProgress = false;
                    EditorGenerationDriver.Stop(this);
                }
            }
        }

        private void OnDrawGizmosSelected()
            => WorldGenerationGizmos.Draw(settings, transform, _debugLogger, _pointGenerator);
#endif
    }
}