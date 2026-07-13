using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NeonImperium.WorldGenerations
{
    [ExecuteInEditMode]
    public class WorldGeneration : MonoBehaviour
    {
        private static int _globalSeedCounter = 0;

        [SerializeField] private int _seed = 0;
        public int Seed 
        { 
            get => _seed; 
            set 
            { 
                _seed = value; 
                _modulesInitialized = false; 
            } 
        }

        public SpawnSettings settings = new();
        public int SpawnedCount => transform.childCount;
        public int ValidPlacementCount => _placementCache.Count;
        public bool IsGenerating => _generationInProgress;
        public int TotalPlacementAttempts => _totalPlacementAttempts;

        public event Action OnGenerationComplete;

        [HideInInspector] private List<PlacementData> _placementCache = new();
        [SerializeField, HideInInspector] private int _totalPlacementAttempts;

        public Dictionary<FailureReasonType, int> FailureStatistics { get; private set; } = new();

        private IPointGenerator _pointGenerator;
        private IPlacementValidator _placementValidator;
        private IPhysicsService _physicsService;
        private IDebugLogger _debugLogger;

        private Awaitable _generationAwaitable;
        private bool _generationInProgress;
        private const int PlacementAttemptsPerFrame = 25;
        private int _runtimeRetryCount;
        private bool _modulesInitialized = false;

        private static List<WorldGeneration> _runtimeSpawners = new();
        private static bool _isRuntimeQueueProcessing = false;

        public List<DebugRay> debugRays => _debugLogger?.GetDebugRays() ?? new List<DebugRay>();

        public void ClearDebugRays() => _debugLogger?.ClearDebugRays();
        public int GetClusterCentersCount() => _pointGenerator is ClusterPointGenerator cpg ? cpg.GetClusterCount() : 0;

        public void AddDebugRay(Vector3 start, Vector3 direction, float distance, Color color, DebugRayType rayType)
        {
            _debugLogger?.AddDebugRay(start, direction, distance, color, rayType);
        }

        private void Awake()
        {
            if (!Application.isPlaying) return;

            if (!settings.enableRuntimeGeneration)
            {
                Destroy(this);
                return;
            }

            EnsureModulesInitialized();

            if (settings.autoGenerateOnStart)
            {
                AddToRuntimeQueue(this);
            }
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            if (!settings.enableRuntimeGeneration || !settings.autoGenerateOnStart) return;

            if (!_isRuntimeQueueProcessing)
            {
                _ = ProcessRuntimeQueueAsync();
            }
        }

        private void OnDestroy()
        {
            StopGeneration();
            if (Application.isPlaying)
            {
                _runtimeSpawners.Remove(this);
            }
#if UNITY_EDITOR
            EditorApplication.update -= AsyncGenerationUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;
#endif
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            settings.population = Math.Max(1, settings.population);
            settings.maxPlacementAttempts = Math.Max(1, settings.maxPlacementAttempts);
            settings.priority = Mathf.Clamp(settings.priority, 0, 100);
            settings.runtimeMaxAttempts = Math.Max(1, settings.runtimeMaxAttempts);
            settings.runtimeRetryCount = Mathf.Clamp(settings.runtimeRetryCount, 0, 10);
            settings.rotationRange.x = Mathf.Clamp(settings.rotationRange.x, 0f, 360f);
            settings.rotationRange.y = Mathf.Clamp(settings.rotationRange.y, 0f, 360f);
            if (settings.rotationRange.x > settings.rotationRange.y)
                settings.rotationRange.x = settings.rotationRange.y;
            settings.scaleRange.x = Mathf.Max(0.1f, settings.scaleRange.x);
            settings.scaleRange.y = Mathf.Max(0.1f, settings.scaleRange.y);
            if (settings.scaleRange.x > settings.scaleRange.y)
                settings.scaleRange.x = settings.scaleRange.y;
            settings.maxHeightDifference = Mathf.Max(0.1f, settings.maxHeightDifference);
            settings.maxRayAngle.x = Mathf.Clamp(settings.maxRayAngle.x, 0f, 90f);
            settings.maxRayAngle.y = Mathf.Clamp(settings.maxRayAngle.y, 0f, 90f);
            if (settings.maxRayAngle.x > settings.maxRayAngle.y)
                settings.maxRayAngle.x = settings.maxRayAngle.y;
            settings.clusterCount = Mathf.Clamp(settings.clusterCount, 1, 100);
            settings.clusterRadiusRange.x = Mathf.Max(1f, settings.clusterRadiusRange.x);
            settings.clusterRadiusRange.y = Mathf.Max(1f, settings.clusterRadiusRange.y);
            if (settings.clusterRadiusRange.x > settings.clusterRadiusRange.y)
                settings.clusterRadiusRange.x = settings.clusterRadiusRange.y;
            settings.objectsPerClusterRange.x = Mathf.Clamp(settings.objectsPerClusterRange.x, 1, 20);
            settings.objectsPerClusterRange.y = Mathf.Clamp(settings.objectsPerClusterRange.y, 1, 20);
            if (settings.objectsPerClusterRange.x > settings.objectsPerClusterRange.y)
                settings.objectsPerClusterRange.x = settings.objectsPerClusterRange.y;
        }

        private void EnsureModulesInitialized()
        {
            if (_modulesInitialized) return;

            _physicsService ??= new PhysicsService();
            _debugLogger ??= new DebugLogger(settings.debugRaySettings);

            int seed = _seed != 0 ? _seed : GetUniqueSeed();
            _placementValidator = new PlacementValidator(seed);

            if (settings.useClustering)
                _pointGenerator = new ClusterPointGenerator();
            else
                _pointGenerator = new RandomPointGenerator();

            _pointGenerator.Initialize(settings, transform, _debugLogger, _physicsService, seed);
            _modulesInitialized = true;
        }

        private int GetUniqueSeed()
        {
            int baseSeed = GetHashCode() ^ (int)(Time.realtimeSinceStartup * 1000) ^ System.Environment.TickCount;
            return baseSeed + (++_globalSeedCounter);
        }

        private void ResetModulesState()
        {
            _debugLogger?.ClearDebugRays();
            _modulesInitialized = false;
            _placementValidator = null;
            _pointGenerator = null;
            EnsureModulesInitialized();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = settings.gizmoColor;
            Gizmos.DrawWireCube(Vector3.zero, settings.dimensions);
            Color transparentColor = new(settings.gizmoColor.r, settings.gizmoColor.g, settings.gizmoColor.b, 0.1f);
            Gizmos.color = transparentColor;
            Gizmos.DrawCube(Vector3.zero, settings.dimensions);
            Gizmos.matrix = Matrix4x4.identity;

            if (settings.debugRaySettings.enabled && _debugLogger != null)
            {
                List<DebugRay> rays = _debugLogger.GetDebugRays();
                for (int i = 0; i < rays.Count; i++)
                {
                    Gizmos.color = rays[i].color;
                    Gizmos.DrawLine(rays[i].start, rays[i].end);
                }
            }

            if (settings.useClustering && _pointGenerator is ClusterPointGenerator cpg)
            {
                List<ClusterPointGenerator.ClusterData> clusters = cpg.GetClusters();
                if (clusters != null)
                {
                    for (int i = 0; i < clusters.Count; i++)
                    {
                        ClusterPointGenerator.ClusterData cluster = clusters[i];
                        Vector3 worldCenter = transform.TransformPoint(cluster.center);
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(worldCenter, 0.5f);
                        Gizmos.color = new Color(0, 1, 0, 0.3f);
                        Gizmos.DrawSphere(worldCenter, cluster.radius);
                    }
                }
            }
        }
#endif

        [ContextMenu("Generate Objects")]
        public void Generate()
        {
            if (_generationInProgress) return;
            if (settings.prefabs.Length == 0)
            {
                Debug.LogError("Префаб не настроен!", this);
                return;
            }
            if (settings.collisionMask.value == 0)
            {
                Debug.LogError("Collision Mask не настроен!", this);
                return;
            }

            ClearAll();
            EnsureModulesInitialized();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.update += AsyncGenerationUpdate;
                SceneView.duringSceneGui += OnSceneGUI;
                _generationInProgress = true;
                Debug.Log($"[{name}] Запущена генерация в редакторе (асинхронно).");
                return;
            }
#endif

            if (Application.isPlaying)
            {
                _ = GenerateAsync();
            }
        }

        public async Awaitable GenerateAsync()
        {
            if (_generationInProgress) return;
            if (settings.prefabs.Length == 0)
            {
                Debug.LogError("Префаб не настроен!", this);
                return;
            }
            if (settings.collisionMask.value == 0)
            {
                Debug.LogError("Collision Mask не настроен!", this);
                return;
            }

            ClearAll();
            EnsureModulesInitialized();

            _generationInProgress = true;
            _runtimeRetryCount = 0;
            bool completed = false;

            
#if UNITY_EDITOR
            Debug.Log($"[{name}] Начинается генерация (Runtime). Seed: {(_seed != 0 ? _seed.ToString() : "случайный")}");
#endif

            while (!completed && _runtimeRetryCount < settings.runtimeRetryCount + 1)
            {
                _placementCache = new List<PlacementData>(settings.population);
                _totalPlacementAttempts = 0;
                FailureStatistics.Clear();
                ClearDebugRays();
                
                if (_runtimeRetryCount > 0)
                {
                    _modulesInitialized = false;
                    EnsureModulesInitialized();
                }

                while (_placementCache.Count < settings.population && (_pointGenerator.HasMorePoints() || !settings.useClustering))
                {
                    if (Application.isPlaying && _totalPlacementAttempts >= settings.runtimeMaxAttempts)
                    {
                        Debug.LogWarning($"[{name}] Достигнут лимит попыток ({settings.runtimeMaxAttempts}), прерываем попытку генерации.");
                        break;
                    }

                    int pointsToGenerate = Mathf.Min(PlacementAttemptsPerFrame, settings.population - _placementCache.Count);
                    GeneratePlacementPointsBatch(pointsToGenerate);
                    await Awaitable.NextFrameAsync();
                }

                if (_placementCache.Count >= settings.population)
                {
                    completed = true;
                    break;
                }
                else
                {
                    _runtimeRetryCount++;
                    if (_runtimeRetryCount < settings.runtimeRetryCount + 1)
                    {
                        Debug.Log($"[{name}] Попытка {_runtimeRetryCount} не достигла цели, перезапуск...");
                        continue;
                    }
                    else
                    {
                        Debug.LogWarning($"[{name}] Исчерпаны все попытки перезапуска ({settings.runtimeRetryCount}). Генерация завершена с {_placementCache.Count} объектами.");
                        completed = true;
                        break;
                    }
                }
            }

            await CreateObjectsFromPlacementData();
            CallGenerationCompleteExtensions();
            OnGenerationComplete?.Invoke();

            if (settings.destroyAfterGeneration && Application.isPlaying)
                Destroy(this);

            _generationInProgress = false;
            Debug.Log($"[{name}] Генерация завершена. Создано объектов: {transform.childCount}");
        }

        private void GeneratePlacementPointsBatch(int desiredCount)
        {
            if (_pointGenerator == null || _placementValidator == null)
            {
                Debug.LogError("Модули не инициализированы! Вызовите EnsureModulesInitialized().");
                return;
            }

            ClearDebugRays();
            int validPoints = 0;
            int attempts = 0;
            int maxAttempts = desiredCount * settings.maxPlacementAttempts;

            while (validPoints < desiredCount && attempts++ < maxAttempts)
            {
                if (_placementCache.Count >= settings.population)
                    break;

                _totalPlacementAttempts++;
                PlacementPoint point = _pointGenerator.GeneratePoint();
                if (point.localPosition == Vector3.zero && settings.useClustering)
                    continue;

                PlacementData placementData = _placementValidator.Validate(point, settings, transform, _placementCache, _debugLogger, _physicsService);
                if (placementData.isValid)
                {
                    _placementCache.Add(placementData);
                    validPoints++;
                }
                else
                {
                    if (FailureStatistics.ContainsKey(placementData.failureReason))
                        FailureStatistics[placementData.failureReason]++;
                    else
                        FailureStatistics[placementData.failureReason] = 1;
                }
            }
        }

        private async Task CreateObjectsFromPlacementData()
        {
            bool wasActive = gameObject.activeSelf;
            if (Application.isPlaying)
            {
                gameObject.SetActive(false);
                await Awaitable.NextFrameAsync();
            }

            int objectsToCreate = Mathf.Min(_placementCache.Count, settings.population);
            for (int i = 0; i < objectsToCreate; i++)
            {
                PlacementData data = _placementCache[i];
                if (!data.isValid || data.prefab == null) continue;

                GameObject newObj;
#if UNITY_EDITOR
                if (Application.isPlaying)
                    newObj = Instantiate(data.prefab, transform);
                else
                    newObj = (GameObject)PrefabUtility.InstantiatePrefab(data.prefab, transform);
#else
                newObj = Instantiate(data.prefab, transform);
#endif

                newObj.transform.SetPositionAndRotation(data.position, data.rotation);
                newObj.transform.localScale = data.scale;
                CallOnSpawnExtensions(newObj);
            }

            if (Application.isPlaying)
            {
                gameObject.SetActive(wasActive);
            }
        }

        private void CallOnSpawnExtensions(GameObject obj)
        {
            for (int i = 0; i < settings.onSpawnExtensions.Count; i++)
            {
                WorldGenerationExtension extension = settings.onSpawnExtensions[i];
                if (extension != null)
                    extension.OnGameObjectSpawned(obj);
            }
        }

        private void CallGenerationCompleteExtensions()
        {
            for (int i = 0; i < settings.onGenerationCompleteExtensions.Count; i++)
            {
                WorldGenerationExtension extension = settings.onGenerationCompleteExtensions[i];
                if (extension != null)
                    extension.OnGenerationComplete();
            }
        }

        [ContextMenu("Clear Objects")]
        public void ClearAll()
        {
            StopGeneration();
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);
            _placementCache.Clear();
            _totalPlacementAttempts = 0;
            _runtimeRetryCount = 0;
            ClearDebugRays();
            FailureStatistics.Clear();
            _modulesInitialized = false;
        }

        private void StopGeneration()
        {
            if (_generationAwaitable != null && !_generationAwaitable.IsCompleted)
            {
                _generationInProgress = false;
                _generationAwaitable = null;
            }
#if UNITY_EDITOR
            EditorApplication.update -= AsyncGenerationUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;
#endif
            _generationInProgress = false;
        }

        private static void AddToRuntimeQueue(WorldGeneration spawner)
        {
            if (spawner == null || _runtimeSpawners.Contains(spawner)) return;
            _runtimeSpawners.Add(spawner);

            if (!_isRuntimeQueueProcessing)
            {
                _ = ProcessRuntimeQueueAsync();
            }
        }

        private static async Awaitable ProcessRuntimeQueueAsync()
        {
            if (_isRuntimeQueueProcessing) return;
            _isRuntimeQueueProcessing = true;

            try
            {
                while (_runtimeSpawners.Count > 0)
                {
                    _runtimeSpawners.Sort((a, b) => b.settings.priority.CompareTo(a.settings.priority));

                    WorldGeneration spawner = _runtimeSpawners[0];
                    if (spawner != null && spawner.enabled && spawner.gameObject.activeInHierarchy)
                    {
                        if (spawner.settings.autoGenerateOnStart && spawner.settings.enableRuntimeGeneration)
                        {
                            await spawner.GenerateAsync();
                        }
                    }
                    _runtimeSpawners.RemoveAt(0);
                }
            }
            finally
            {
                _isRuntimeQueueProcessing = false;
            }
        }

#if UNITY_EDITOR
        private async void AsyncGenerationUpdate()
        {
            if (!_generationInProgress) return;
            EnsureModulesInitialized();
            if (_pointGenerator == null || _placementValidator == null)
            {
                Debug.LogError("Модули не инициализированы в AsyncGenerationUpdate.");
                StopGeneration();
                return;
            }
            if (_placementCache.Count < settings.population)
            {
                GeneratePlacementPointsBatch(PlacementAttemptsPerFrame);
            }
            else
            {
                await CreateObjectsFromPlacementData();
                CallGenerationCompleteExtensions();
                OnGenerationComplete?.Invoke();
                StopGeneration();
                Debug.Log($"[{name}] Генерация в редакторе завершена. Создано объектов: {transform.childCount}");
            }
            SceneView.RepaintAll();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_generationInProgress) sceneView.Repaint();
            else SceneView.duringSceneGui -= OnSceneGUI;
        }
#endif
    }
}