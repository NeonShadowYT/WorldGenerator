using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NeonImperium.WorldGeneration
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
        
        public Dictionary<FailureReasonType, int> FailureStatistics { get; private set; } = new();
        
        private IPlacementStrategy placementStrategy;
        private Coroutine generationCoroutine;
        private bool generationInProgress;
        private const int PlacementAttemptsPerFrame = 25;
        
        private List<ClusterData> clusters;
        private int totalObjectsPlacedInClusters;
        
        [System.Serializable]
        public class ClusterData
        {
            public Vector3 center;
            public float radius;
            public int targetObjects;
            public int placedObjects;
            public bool isActive = true;
        }

        public List<DebugRay> debugRays = new();

        public void ClearDebugRays() => debugRays.Clear();
        public int GetClusterCentersCount() => clusters?.Count ?? 0;

        public void AddDebugRay(Vector3 start, Vector3 direction, float distance, Color color, DebugRayType rayType)
        {
            if (!settings.debugRaySettings.enabled) return;
            
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
            
            settings.population = Math.Max(1, settings.population);
            settings.maxPlacementAttempts = Math.Max(1, settings.maxPlacementAttempts);
            settings.priority = Mathf.Clamp(settings.priority, 0, 100);
            
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
            
            placementStrategy = new RandomPlacement();
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = settings.gizmoColor;
            Gizmos.DrawWireCube(Vector3.zero, settings.dimensions);
            
            Color transparentColor = new Color(settings.gizmoColor.r, settings.gizmoColor.g, 
                                          settings.gizmoColor.b, 0.1f);
            Gizmos.color = transparentColor;
            Gizmos.DrawCube(Vector3.zero, settings.dimensions);
            
            Gizmos.matrix = Matrix4x4.identity;
            
            if (settings.debugRaySettings.enabled)
            {
                for (int i = 0; i < debugRays.Count; i++)
                {
                    Gizmos.color = debugRays[i].color;
                    Gizmos.DrawLine(debugRays[i].start, debugRays[i].end);
                }
            }
            
            if (settings.useClustering && clusters != null)
            {
                for (int i = 0; i < clusters.Count; i++)
                {
                    ClusterData cluster = clusters[i];
                    Vector3 worldCenter = transform.TransformPoint(cluster.center);
                    
                    if (cluster.isActive)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(worldCenter, 0.5f);
                        Gizmos.color = new Color(0, 1, 0, 0.3f);
                        Gizmos.DrawSphere(worldCenter, cluster.radius);
                        
                        GUIStyle labelStyle = new GUIStyle();
                        labelStyle.normal.textColor = Color.white;
                        labelStyle.fontSize = 10;
                        labelStyle.alignment = TextAnchor.MiddleCenter;
                        
                        Handles.Label(worldCenter + Vector3.up * 2f, 
                            $"Кластер {i}\n{cluster.placedObjects}/{cluster.targetObjects}", labelStyle);
                    }
                    else
                    {
                        Gizmos.color = Color.gray;
                        Gizmos.DrawWireSphere(worldCenter, 0.5f);
                        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                        Gizmos.DrawSphere(worldCenter, cluster.radius);
                        
                        GUIStyle labelStyle = new GUIStyle();
                        labelStyle.normal.textColor = Color.gray;
                        labelStyle.fontSize = 10;
                        labelStyle.alignment = TextAnchor.MiddleCenter;
                        
                        Handles.Label(worldCenter + Vector3.up * 2f, 
                            $"Кластер {i}\nНеактивен", labelStyle);
                    }
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
            clusters = null;
            totalObjectsPlacedInClusters = 0;
            ClearDebugRays();
            FailureStatistics.Clear();
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
            FailureStatistics.Clear();
            
            while (placementCache.Count < settings.population)
            {
                int pointsToGenerate = Mathf.Min(PlacementAttemptsPerFrame, 
                    settings.population - placementCache.Count);
                GeneratePlacementPointsBatch(pointsToGenerate);
                yield return null;
            }
            
            CreateObjectsFromPlacementData();
            CallGenerationCompleteExtensions();
            generationInProgress = false;
        }

        private void GeneratePlacementPointsBatch(int desiredCount)
        {
            ClearDebugRays();
            int validPoints = 0;
            int attempts = 0;
            int maxAttempts = desiredCount * settings.maxPlacementAttempts;

            if (settings.useClustering && clusters == null)
            {
                GenerateClusters();
            }

            while (validPoints < desiredCount && attempts++ < maxAttempts)
            {
                if (placementCache.Count >= settings.population)
                    break;
                    
                totalPlacementAttempts++;
                
                PlacementPoint point;
                if (settings.useClustering && totalObjectsPlacedInClusters < settings.population)
                {
                    point = GeneratePointInActiveCluster();
                    if (point.localPosition == Vector3.zero)
                    {
                        continue;
                    }
                }
                else
                {
                    point = placementStrategy.GeneratePoint(settings, transform);
                }
                
                PlacementData placementData = PlacementCalculator.CalculatePoint(
                    point, settings, transform, placementCache, this
                );
                
                if (placementData.isValid)
                {
                    if (placementCache.Count < settings.population)
                    {
                        placementCache.Add(placementData);
                        validPoints++;
                        
                        if (settings.useClustering)
                        {
                            totalObjectsPlacedInClusters++;
                            UpdateActiveClusters();
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
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
        
        private void GenerateClusters()
        {
            clusters = new List<ClusterData>();
            totalObjectsPlacedInClusters = 0;
            
            float halfX = settings.dimensions.x * 0.5f;
            float halfZ = settings.dimensions.z * 0.5f;
            int attempts = 0;
            int maxClusterAttempts = settings.clusterCount * 100;
            
            int clustersCreated = 0;
            int targetClusters = Mathf.Min(settings.clusterCount, settings.population);
            
            while (clustersCreated < targetClusters && attempts++ < maxClusterAttempts)
            {
                Vector3 localCandidate = new Vector3(
                    UnityEngine.Random.Range(-halfX, halfX),
                    0,
                    UnityEngine.Random.Range(-halfZ, halfZ)
                );
                
                Vector3 worldCandidate = transform.TransformPoint(localCandidate);
                
                Vector3 rayStart = worldCandidate + Vector3.up * settings.dimensions.y;
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, settings.dimensions.y * 2, settings.collisionMask))
                {
                    Vector3 surfacePosition = hit.point;
                    Vector3 localSurface = transform.InverseTransformPoint(surfacePosition);
                    
                    bool validPosition = true;
                    for (int i = 0; i < clusters.Count; i++)
                    {
                        if (Vector3.Distance(localSurface, clusters[i].center) < settings.minDistanceBetweenClusters)
                        {
                            validPosition = false;
                            break;
                        }
                    }
                    
                    if (validPosition)
                    {
                        ClusterData newCluster = new ClusterData
                        {
                            center = localSurface,
                            radius = UnityEngine.Random.Range(
                                settings.clusterRadiusRange.x,
                                settings.clusterRadiusRange.y
                            ),
                            targetObjects = UnityEngine.Random.Range(
                                settings.objectsPerClusterRange.x,
                                settings.objectsPerClusterRange.y + 1
                            ),
                            placedObjects = 0,
                            isActive = true
                        };
                        
                        clusters.Add(newCluster);
                        clustersCreated++;
                    }
                }
            }
            
            if (clusters.Count < targetClusters)
            {
                Debug.LogWarning($"Создано только {clusters.Count}/{targetClusters} кластеров. Увеличьте зону спавна или уменьшите minDistanceBetweenClusters.");
                if (FailureStatistics.ContainsKey(FailureReasonType.ClusterFailed))
                {
                    FailureStatistics[FailureReasonType.ClusterFailed] += targetClusters - clusters.Count;
                }
                else
                {
                    FailureStatistics[FailureReasonType.ClusterFailed] = targetClusters - clusters.Count;
                }
            }
            
            int totalTargetObjects = 0;
            for (int i = 0; i < clusters.Count; i++)
            {
                totalTargetObjects += clusters[i].targetObjects;
            }
            
            if (totalTargetObjects < settings.population)
            {
                Debug.LogWarning($"Целевое количество объектов в кластерах ({totalTargetObjects}) меньше требуемого ({settings.population}). Увеличьте objectsPerClusterRange.");
            }
            else if (totalTargetObjects > settings.population * 2)
            {
                Debug.LogWarning($"Целевое количество объектов в кластерах ({totalTargetObjects}) значительно превышает требуемое ({settings.population}). Уменьшите objectsPerClusterRange.");
            }
        }
        
        private PlacementPoint GeneratePointInActiveCluster()
        {
            List<ClusterData> activeClusters = new List<ClusterData>();
            for (int i = 0; i < clusters.Count; i++)
            {
                if (clusters[i].isActive && clusters[i].placedObjects < clusters[i].targetObjects)
                {
                    activeClusters.Add(clusters[i]);
                }
            }
            
            if (activeClusters.Count == 0)
            {
                return new PlacementPoint { localPosition = Vector3.zero };
            }
            
            ClusterData selectedCluster = activeClusters[UnityEngine.Random.Range(0, activeClusters.Count)];
            
            PlacementPoint point = new PlacementPoint();
            int attempts = 0;
            const int maxAttempts = 20;
            
            while (attempts++ < maxAttempts)
            {
                Vector2 randomPoint = UnityEngine.Random.insideUnitCircle * selectedCluster.radius;
                point.localPosition = selectedCluster.center + new Vector3(randomPoint.x, 0, randomPoint.y);
                
                Vector3 worldPos = transform.TransformPoint(point.localPosition);
                
                if (PlacementCalculator.IsValidClusterCenter(worldPos, settings, transform))
                {
                    selectedCluster.placedObjects++;
                    return point;
                }
            }
            
            return new PlacementPoint { localPosition = Vector3.zero };
        }
        
        private void UpdateActiveClusters()
        {
            bool allClustersFull = true;
            for (int i = 0; i < clusters.Count; i++)
            {
                if (clusters[i].placedObjects >= clusters[i].targetObjects)
                {
                    clusters[i].isActive = false;
                }
                else
                {
                    clusters[i].isActive = true;
                    allClustersFull = false;
                }
            }
            
            if (allClustersFull && totalObjectsPlacedInClusters < settings.population)
            {
                Debug.LogWarning($"Все кластеры заполнены, но целевое количество объектов ({settings.population}) не достигнуто. Увеличьте objectsPerClusterRange.");
            }
        }

        private void CreateObjectsFromPlacementData()
        {
            int objectsToCreate = Mathf.Min(placementCache.Count, settings.population);
            
            for (int i = 0; i < objectsToCreate; i++)
            {
                PlacementData data = placementCache[i];
                if (!data.isValid || data.prefab == null) continue;
                
                GameObject newObj = Application.isPlaying ? 
                    Instantiate(data.prefab, transform) : 
                    (GameObject)PrefabUtility.InstantiatePrefab(data.prefab, transform);
                
                newObj.transform.SetPositionAndRotation(data.position, data.rotation);
                newObj.transform.localScale = data.scale;

                CallOnSpawnExtensions(newObj);
            }
            
            if (settings.useClustering && clusters != null)
            {
                Debug.Log($"Кластеризация завершена: создано {placementCache.Count} объектов в {clusters.Count} кластерах");
                
                int emptyClusters = 0;
                int overfilledClusters = 0;
                int totalObjectsInClusters = 0;
                
                for (int i = 0; i < clusters.Count; i++)
                {
                    totalObjectsInClusters += clusters[i].placedObjects;
                    
                    if (clusters[i].placedObjects == 0)
                    {
                        emptyClusters++;
                    }
                    else if (clusters[i].placedObjects > clusters[i].targetObjects)
                    {
                        overfilledClusters++;
                    }
                }
                
                if (emptyClusters > 0)
                {
                    Debug.LogWarning($"⚠️ {emptyClusters} кластеров остались пустыми. Уменьшите clusterRadiusRange или objectsPerClusterRange.");
                }
                
                if (overfilledClusters > 0)
                {
                    Debug.LogWarning($"⚠️ {overfilledClusters} кластеров переполнены. Увеличьте clusterRadiusRange или objectsPerClusterRange.");
                }
                
                if (totalObjectsInClusters < settings.population * 0.7f)
                {
                    Debug.LogWarning($"⚠️ Кластеризация неэффективна: создано только {totalObjectsInClusters} объектов из {settings.population}. Увеличьте clusterCount или objectsPerClusterRange.");
                }
            }
        }

        private void CallOnSpawnExtensions(GameObject obj)
        {
            for (int i = 0; i < settings.onSpawnExtensions.Count; i++)
            {
                WorldGenerationExtension extension = settings.onSpawnExtensions[i];
                if (extension != null)
                {
                    extension.OnGameObjectSpawned(obj);
                }
            }
        }

        private void CallGenerationCompleteExtensions()
        {
            for (int i = 0; i < settings.onGenerationCompleteExtensions.Count; i++)
            {
                WorldGenerationExtension extension = settings.onGenerationCompleteExtensions[i];
                if (extension != null)
                {
                    extension.OnGenerationComplete();
                }
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
                CallGenerationCompleteExtensions();
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