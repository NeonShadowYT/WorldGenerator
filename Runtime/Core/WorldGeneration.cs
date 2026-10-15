using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;
using System.Threading;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Alias вместо `using System.Diagnostics;` — иначе конфликт UnityEngine.Debug <-> System.Diagnostics.Debug.
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

        // ─────────────────────── Lifecycle ───────────────────────

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

            // Валидатор владеет PlacementJobs — dispose'им через его интерфейс.
            _placementValidator?.Dispose();
            _placementValidator = null;

            _generationCts?.Dispose();
            _generationCts = null;
        }

        private void OnValidate()
        {
            _previewCache.Clear();
            settings?.Clamp();
            _modulesInitialized = false;
        }

        private void EnsureModulesInitialized()
        {
            if (_modulesInitialized) return;

            // Пересоздаём валидатор — старый освобождает свои persistent-буферы.
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

        // ─────────────────────── Public API ───────────────────────

        [ContextMenu("Generate Objects")]
        public void Generate()
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

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                _generationInProgress = true;
                EditorGenerationDriver.Start(this);
                UnityEngine.Debug.Log($"[{name}] Запущена генерация в редакторе (асинхронно).");
                return;
            }
#endif
            if (Application.isPlaying) _ = GenerateAsync();
        }

        /// <summary>Preview-прогон: N попыток валидации без Instantiate.</summary>
        public void GeneratePreview()
        {
            _previewCache.Clear();
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
                _previewCache.Add(_placementValidator.Validate(point, transform, _previewCache,
                    _debugLogger, _physicsService, null));
            }
        }

        public void ClearPreview() => _previewCache.Clear();

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

            _generationInProgress = true;
            _runtimeRetryCount = 0;
            bool completed = false;

            try
            {
                while (!completed && _runtimeRetryCount < settings.runtimeRetryCount + 1)
                {
                    token.ThrowIfCancellationRequested();

                    _placementCache = new List<PlacementData>(settings.population);
                    _totalPlacementAttempts = 0;
                    FailureStatistics.Clear();
                    ClearDebugRays();
                    _placementHash?.Clear();

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
                await TryRuntimeNavMeshRegenerationAsync(token);

                CallGenerationCompleteExtensions();
                OnGenerationComplete?.Invoke();

                if (settings.destroyAfterGeneration && Application.isPlaying)
                    Destroy(this);
            }
            catch (OperationCanceledException)
            {
                // штатная отмена — тихо выходим
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e, this);
            }
            finally
            {
                _generationInProgress = false;
            }
        }

        // ─────────────────────── Batch generation ───────────────────────

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

        /// <summary>Job-путь: собрать кандидатов → ValidateBatch → разобрать результат.</summary>
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

            for (int i = 0; i < count; i++)
            {
                PlacementData data = _batchOutput[i];
                if (data.isValid)
                {
                    _placementCache.Add(data);
                    _placementHash?.Insert(data.position, _placementCache.Count - 1);
                }
                else RecordFailure(data.failureReason);
            }
        }

        private void RecordFailure(FailureReasonType reason)
        {
            FailureStatistics.TryGetValue(reason, out int count);
            FailureStatistics[reason] = count + 1;
        }

        // ─────────────────────── Instantiating ───────────────────────

        private async Awaitable CreateObjectsFromPlacementData(CancellationToken token)
        {
            bool wasActive = gameObject.activeSelf;
            if (Application.isPlaying)
            {
                gameObject.SetActive(false);
                await Awaitable.NextFrameAsync(token);
            }

            await ObjectInstantiator.InstantiateBatchAsync(_placementCache, transform, settings.population,
                settings.onSpawnExtensions, settings.reuseExistingObjects, token);

            if (Application.isPlaying) gameObject.SetActive(wasActive);
        }

        // ─────────────────────── NavMesh ───────────────────────

        private async Awaitable TryRuntimeNavMeshRegenerationAsync(CancellationToken token)
        {
            if (!Application.isPlaying) return;
            if (!settings.requestNavMeshRegeneration) return;

            if (NavMeshRegenerationService.HasNavMeshSurface())
            {
                int rebuilt = NavMeshRegenerationService.RebuildAllSurfacesSync();
                if (rebuilt > 0)
                {
                    UnityEngine.Debug.Log(
                        $"[{name}] Runtime NavMesh перегенерирован через NavMeshSurface: {rebuilt} surface(ов).");
                    return;
                }
            }

            NavMeshData data = settings.runtimeNavMeshData;
            if (data == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[{name}] requestNavMeshRegeneration=true, но runtimeNavMeshData не назначен и NavMeshSurface не найден.\n" +
                    "Создайте NavMeshSurface (AI Navigation) → Bake → сохраните NavMeshData как .asset и назначьте в runtimeNavMeshData.");
                return;
            }

            if (_runtimeNavMeshSources == null)
                _runtimeNavMeshSources = new List<NavMeshBuildSource>(256);

            Bounds bounds = GetRuntimeBounds();

            AsyncOperation op = RuntimeNavMeshBuilder.RegenerateAsync(
                data, bounds, NavMeshCollectGeometry.PhysicsColliders,
                settings.collisionMask.value, settings.runtimeAgentTypeID, _runtimeNavMeshSources);

            if (op == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[{name}] Runtime NavMesh regeneration не запустилась (data/sources = null).");
                return;
            }

            while (!op.isDone)
            {
                token.ThrowIfCancellationRequested();
                await Awaitable.NextFrameAsync(token);
            }

            UnityEngine.Debug.Log(
                $"[{name}] Runtime NavMesh перегенерирован (agentTypeID={settings.runtimeAgentTypeID}).");
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

        // ─────────────────────── Clear / Reset ───────────────────────

        [ContextMenu("Clear Objects")]
        public void ClearAll()
        {
            StopGeneration();
            while (transform.childCount > 0) DestroyImmediate(transform.GetChild(0).gameObject);
            ResetGenerationState();
        }

        /// <summary>Сброс кэшей без удаления дочерних объектов (reuseExistingObjects).</summary>
        private void ClearGenerationState()
        {
            StopGeneration();
            ResetGenerationState();
        }

        private void ResetGenerationState()
        {
            _placementCache.Clear();
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
            _generationCts?.Cancel();

#if UNITY_EDITOR
            EditorGenerationDriver.Stop(this);
#endif

            _generationInProgress = false;
        }

        // ─────────────────────── Editor-only ───────────────────────

#if UNITY_EDITOR
        /// <summary>Один тик редакторной генерации. Вызывается EditorGenerationDriver.</summary>
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
                GeneratePlacementPointsBatch(GetFrameBatchSize());
            else
                _ = FinishEditorGenerationAsync();
        }

        private async Awaitable FinishEditorGenerationAsync()
        {
            try
            {
                await CreateObjectsFromPlacementData(CancellationToken.None);
                CallGenerationCompleteExtensions();
                OnGenerationComplete?.Invoke();
                StopGeneration();
                UnityEngine.Debug.Log($"[{name}] Генерация завершена. Создано: {transform.childCount}");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e, this);
                StopGeneration();
            }
        }

        private void OnDrawGizmosSelected()
            => WorldGenerationGizmos.Draw(settings, transform, _debugLogger, _pointGenerator);
#endif
    }
}