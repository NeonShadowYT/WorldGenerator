using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NeonImperium
{
    [ExecuteInEditMode]
    public class WorldGeneration : MonoBehaviour
    {
        public SpawnSettings settings = new();
        public int SpawnedCount => transform.childCount;
        public int ValidPlacementCount => placementCache.Count;
        public bool IsGenerating => generationInProgress;
        public int TotalPlacementAttempts => totalPlacementAttempts;
        
        [SerializeField, HideInInspector] private List<PlacementData> placementCache = new();
        [SerializeField, HideInInspector] private int totalPlacementAttempts;
        
        // Новая статистика ошибок
        public Dictionary<FailureReasonType, int> FailureStatistics { get; private set; } = new();
        
        private IPlacementStrategy placementStrategy;
        private Coroutine generationCoroutine;
        private bool generationInProgress;
        private const int PlacementAttemptsPerFrame = 25;
        
        private List<Vector3> clusterCenters;
        private int currentClusterIndex;
        private int objectsInCurrentCluster;
        private int targetObjectsInCluster;

        public List<DebugRay> debugRays = new();

        public void ClearDebugRays() => debugRays.Clear();
        public int GetClusterCentersCount() => clusterCenters?.Count ?? 0;

        public void AddDebugRay(Vector3 start, Vector3 direction, float distance, Color color, DebugRayType rayType)
        {
            if (!settings.debugRaySettings.enabled) return;
            
            // Проверяем, нужно ли отображать этот тип луча
            bool shouldDraw = rayType switch
            {
                DebugRayType.Main => settings.debugRaySettings.showMainRays,
                DebugRayType.Stability => settings.debugRaySettings.showStabilityRays,
                DebugRayType.Floor => settings.debugRaySettings.showFloorRays,
                DebugRayType.Avoidance => settings.debugRaySettings.showAvoidanceRays,
                DebugRayType.Ceiling => settings.debugRaySettings.showCeilingRays,
                _ => true
            };

            if (!shouldDraw) return;

            debugRays.Add(new DebugRay
            {
                start = start,
                end = start + direction * distance,
                color = color,
                rayType = rayType
            });
        }

        private void Start()
        {
            if (Application.isPlaying) Destroy(this);
        }
        
        private void OnDestroy()
        {
            StopAllCoroutines();
            #if UNITY_EDITOR
            EditorApplication.update -= AsyncGenerationUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;
            #endif
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            
            // Ограничения для настроек
            settings.population = Math.Max(1, settings.population);
            settings.maxPlacementAttempts = Math.Max(1, settings.maxPlacementAttempts);
            
            // Ограничения для rotationRange
            settings.rotationRange.x = Mathf.Clamp(settings.rotationRange.x, 0f, 360f);
            settings.rotationRange.y = Mathf.Clamp(settings.rotationRange.y, 0f, 360f);
            if (settings.rotationRange.x > settings.rotationRange.y)
                settings.rotationRange.x = settings.rotationRange.y;
            
            // Ограничения для scaleRange
            settings.scaleRange.x = Mathf.Max(0.1f, settings.scaleRange.x);
            settings.scaleRange.y = Mathf.Max(0.1f, settings.scaleRange.y);
            if (settings.scaleRange.x > settings.scaleRange.y)
                settings.scaleRange.x = settings.scaleRange.y;

            settings.maxHeightDifference = Mathf.Max(0.1f, settings.maxHeightDifference);
            
            // Ограничения для maxRayAngle
            settings.maxRayAngle.x = Mathf.Clamp(settings.maxRayAngle.x, 0f, 90f);
            settings.maxRayAngle.y = Mathf.Clamp(settings.maxRayAngle.y, 0f, 90f);
            if (settings.maxRayAngle.x > settings.maxRayAngle.y)
                settings.maxRayAngle.x = settings.maxRayAngle.y;
            
            // Ограничения для кластеризации
            settings.clusterCount = Mathf.Clamp(settings.clusterCount, 1, 100);
            settings.clusterRadiusRange.x = Mathf.Max(1f, settings.clusterRadiusRange.x);
            settings.clusterRadiusRange.y = Mathf.Max(1f, settings.clusterRadiusRange.y);
            if (settings.clusterRadiusRange.x > settings.clusterRadiusRange.y)
                settings.clusterRadiusRange.x = settings.clusterRadiusRange.y;
            
            settings.objectsPerClusterRange.x = Mathf.Clamp(settings.objectsPerClusterRange.x, 1, 20);
            settings.objectsPerClusterRange.y = Mathf.Clamp(settings.objectsPerClusterRange.y, 1, 20);
            if (settings.objectsPerClusterRange.x > settings.objectsPerClusterRange.y)
                settings.objectsPerClusterRange.x = settings.objectsPerClusterRange.y;
            
            placementStrategy = new RandomPlacement();
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = settings.gizmoColor;
            Gizmos.DrawWireCube(Vector3.zero, settings.dimensions);
            
            var transparentColor = new Color(settings.gizmoColor.r, settings.gizmoColor.g, 
                                          settings.gizmoColor.b, 0.1f);
            Gizmos.color = transparentColor;
            Gizmos.DrawCube(Vector3.zero, settings.dimensions);
            
            Gizmos.matrix = Matrix4x4.identity;
            
            // Отрисовываем лучи только если включена настройка
            if (settings.debugRaySettings.enabled)
            {
                foreach (var ray in debugRays)
                {
                    Gizmos.color = ray.color;
                    Gizmos.DrawLine(ray.start, ray.end);
                }
            }
            
            if (settings.useClustering && clusterCenters != null)
            {
                foreach (var center in clusterCenters)
                {
                    Vector3 worldCenter = transform.TransformPoint(center);
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(worldCenter, 0.5f);
                    Gizmos.color = new Color(0, 1, 0, 0.3f);
                    Gizmos.DrawSphere(worldCenter, settings.clusterRadiusRange.y);
                }
            }
        }

        [ContextMenu("Generate Objects")]
        public void GenerateObjects()
        {
            if (generationInProgress) return;

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
            SceneView.duringSceneGui += OnSceneGUI;
            
            if (Application.isPlaying)
            {
                generationCoroutine = StartCoroutine(GenerateObjectsAsync());
            }
            else
            {
                EditorApplication.update += AsyncGenerationUpdate;
                generationInProgress = true;
            }
        }

        [ContextMenu("Clear Objects")]
        public void ClearAll()
        {
            StopGeneration();
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);
            
            placementCache.Clear();
            totalPlacementAttempts = 0;
            clusterCenters = null;
            ClearDebugRays();
            FailureStatistics.Clear(); // Очищаем статистику
        }

        private void StopGeneration()
        {
            if (generationCoroutine != null) StopCoroutine(generationCoroutine);
            generationCoroutine = null;
            
            EditorApplication.update -= AsyncGenerationUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;
            generationInProgress = false;
        }

        private IEnumerator GenerateObjectsAsync()
        {
            generationInProgress = true;
            placementCache = new List<PlacementData>(settings.population);
            FailureStatistics.Clear(); // Сбрасываем статистику перед генерацией
            
            while (placementCache.Count < settings.population)
            {
                int pointsToGenerate = Mathf.Min(PlacementAttemptsPerFrame, 
                    settings.population - placementCache.Count);
                GeneratePlacementPointsBatch(pointsToGenerate);
                yield return null;
            }
            
            CreateObjectsFromPlacementData();
            generationInProgress = false;
        }

        private void GeneratePlacementPointsBatch(int desiredCount)
        {
            ClearDebugRays();
            int validPoints = 0;
            int attempts = 0;
            int maxAttempts = desiredCount * settings.maxPlacementAttempts;

            if (settings.useClustering && clusterCenters == null)
            {
                GenerateClusterCenters();
            }

            while (validPoints < desiredCount && attempts++ < maxAttempts)
            {
                // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Проверяем, не достигли ли мы уже лимита
                if (placementCache.Count >= settings.population)
                    break;
                    
                totalPlacementAttempts++;
                
                PlacementPoint point;
                if (settings.useClustering && currentClusterIndex < clusterCenters.Count)
                {
                    point = GeneratePointInCluster();
                }
                else
                {
                    point = placementStrategy.GeneratePoint(settings, transform);
                }
                
                var placementData = PlacementCalculator.CalculatePoint(
                    point, settings, transform, placementCache, this
                );
                
                if (placementData.isValid)
                {
                    // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА: Убеждаемся, что не превышаем лимит
                    if (placementCache.Count < settings.population)
                    {
                        placementCache.Add(placementData);
                        validPoints++;
                    }
                    else
                    {
                        break; // Достигли лимита - выходим
                    }
                }
                else
                {
                    // Обновляем статистику ошибок
                    if (FailureStatistics.ContainsKey(placementData.failureReason))
                    {
                        FailureStatistics[placementData.failureReason]++;
                    }
                    else
                    {
                        FailureStatistics[placementData.failureReason] = 1;
                    }
                }
            }
        }
        
        private void GenerateClusterCenters()
        {
            clusterCenters = new List<Vector3>();
            currentClusterIndex = 0;
            objectsInCurrentCluster = 0;
            
            float halfX = settings.dimensions.x * 0.5f;
            float halfZ = settings.dimensions.z * 0.5f;
            int attempts = 0;
            int maxClusterAttempts = settings.clusterCount * 50;
            
            while (clusterCenters.Count < settings.clusterCount && attempts++ < maxClusterAttempts)
            {
                Vector3 localCandidate = new(
                    UnityEngine.Random.Range(-halfX, halfX),
                    0,
                    UnityEngine.Random.Range(-halfZ, halfZ)
                );
                
                Vector3 worldCandidate = transform.TransformPoint(localCandidate);
                
                // Получаем точку на поверхности с помощью луча
                Vector3 rayStart = worldCandidate + Vector3.up * settings.dimensions.y;
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, settings.dimensions.y * 2, settings.collisionMask))
                {
                    // Используем точку попадания луча как центр кластера
                    Vector3 surfacePosition = hit.point;
                    Vector3 localSurface = transform.InverseTransformPoint(surfacePosition);
                    
                    bool valid = true;
                    foreach (var existing in clusterCenters)
                    {
                        if (Vector3.Distance(localSurface, existing) < settings.minDistanceBetweenClusters)
                        {
                            valid = false;
                            break;
                        }
                    }
                    
                    if (valid) clusterCenters.Add(localSurface);
                }
            }
            
            if (clusterCenters.Count < settings.clusterCount)
            {
                Debug.LogWarning($"Created only {clusterCenters.Count}/{settings.clusterCount} clusters.");
                // Записываем ошибку кластеризации
                if (FailureStatistics.ContainsKey(FailureReasonType.ClusterFailed))
                    FailureStatistics[FailureReasonType.ClusterFailed]++;
                else
                    FailureStatistics[FailureReasonType.ClusterFailed] = 1;
            }
            
            if (clusterCenters.Count > 0)
            {
                targetObjectsInCluster = UnityEngine.Random.Range(
                    settings.objectsPerClusterRange.x,
                    settings.objectsPerClusterRange.y + 1
                );
            }
        }
        
        private PlacementPoint GeneratePointInCluster()
        {
            Vector3 center = clusterCenters[currentClusterIndex];
            float radius = UnityEngine.Random.Range(
                settings.clusterRadiusRange.x,
                settings.clusterRadiusRange.y
            );
            
            PlacementPoint point = new();
            int attempts = 0;
            const int maxAttempts = 10;
            
            while (attempts++ < maxAttempts)
            {
                Vector2 randomPoint = UnityEngine.Random.insideUnitCircle * radius;
                point.localPosition = center + new Vector3(randomPoint.x, 0, randomPoint.y);
                
                Vector3 worldPos = transform.TransformPoint(point.localPosition);
                
                // Проверяем точку по тем же правилам, что и центры
                if (PlacementCalculator.IsValidClusterCenter(worldPos, settings, transform))
                    break;
            }
            
            objectsInCurrentCluster++;
            if (objectsInCurrentCluster >= targetObjectsInCluster && 
                currentClusterIndex < clusterCenters.Count - 1)
            {
                currentClusterIndex++;
                objectsInCurrentCluster = 0;
                targetObjectsInCluster = UnityEngine.Random.Range(
                    settings.objectsPerClusterRange.x,
                    settings.objectsPerClusterRange.y + 1
                );
            }
            
            return point;
        }

        private void CreateObjectsFromPlacementData()
        {
            // ДОПОЛНИТЕЛЬНАЯ ЗАЩИТА: Ограничиваем количество создаваемых объектов
            int objectsToCreate = Mathf.Min(placementCache.Count, settings.population);
            
            for (int i = 0; i < objectsToCreate; i++)
            {
                var data = placementCache[i];
                if (!data.isValid || data.prefab == null) continue;
                
                GameObject newObj = Application.isPlaying ? 
                    Instantiate(data.prefab, transform) : 
                    (GameObject)PrefabUtility.InstantiatePrefab(data.prefab, transform);
                
                newObj.transform.SetPositionAndRotation(data.position, data.rotation);
                newObj.transform.localScale = data.scale;
            }
        }

        private void AsyncGenerationUpdate()
        {
            if (!generationInProgress) return;
            
            if (placementCache.Count < settings.population)
            {
                GeneratePlacementPointsBatch(PlacementAttemptsPerFrame);
            }
            else
            {
                CreateObjectsFromPlacementData();
                StopGeneration();
            }
            SceneView.RepaintAll();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (generationInProgress) sceneView.Repaint();
            else SceneView.duringSceneGui -= OnSceneGUI;
        }
        #endif
    }
}