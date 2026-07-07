using UnityEngine;
using System.Collections.Generic;

namespace NeonImperium.WorldGeneration
{
    public class RandomPointGenerator : IPointGenerator
    {
        private SpawnSettings _settings;
        private Transform _spawner;
        private System.Random _random;

        public void Initialize(SpawnSettings settings, Transform spawner, IDebugLogger debugLogger, IPhysicsService physicsService, int seed)
        {
            _settings = settings;
            _spawner = spawner;
            _random = new System.Random(seed);
        }

        public PlacementPoint GeneratePoint()
        {
            float halfX = _settings.dimensions.x * 0.5f;
            float halfZ = _settings.dimensions.z * 0.5f;
            float x = (float)(_random.NextDouble() * 2.0 - 1.0) * halfX;
            float z = (float)(_random.NextDouble() * 2.0 - 1.0) * halfZ;
            return new PlacementPoint { localPosition = new Vector3(x, 0, z) };
        }

        public bool HasMorePoints() => true;
        public int GetClusterCount() => 0;
    }

    public class ClusterPointGenerator : IPointGenerator
    {
        public class ClusterData
        {
            public Vector3 center;
            public float radius;
        }

        private SpawnSettings _settings;
        private Transform _spawner;
        private IPhysicsService _physicsService;
        private List<ClusterData> _clusters;
        private System.Random _random;

        public void Initialize(SpawnSettings settings, Transform spawner, IDebugLogger debugLogger, IPhysicsService physicsService, int seed)
        {
            _settings = settings;
            _spawner = spawner;
            _physicsService = physicsService;
            _random = new System.Random(seed);
            _clusters = new List<ClusterData>();
            GenerateClusters();
        }

        private void GenerateClusters()
        {
            _clusters.Clear();
            float halfX = _settings.dimensions.x * 0.5f;
            float halfZ = _settings.dimensions.z * 0.5f;
            int attempts = 0;
            int maxClusterAttempts = _settings.clusterCount * 100;
            int clustersCreated = 0;
            int targetClusters = Mathf.Min(_settings.clusterCount, _settings.population);

            RaycastHit[] hitBuffer = new RaycastHit[1];

            while (clustersCreated < targetClusters && attempts++ < maxClusterAttempts)
            {
                float x = (float)(_random.NextDouble() * 2.0 - 1.0) * halfX;
                float z = (float)(_random.NextDouble() * 2.0 - 1.0) * halfZ;
                Vector3 localCandidate = new Vector3(x, 0, z);
                Vector3 worldCandidate = _spawner.TransformPoint(localCandidate);
                Vector3 rayStart = worldCandidate + Vector3.up * _settings.dimensions.y;

                int hitCount = _physicsService.RaycastNonAlloc(rayStart, Vector3.down, hitBuffer, _settings.dimensions.y * 2, _settings.collisionMask);
                if (hitCount > 0)
                {
                    Vector3 surfacePosition = hitBuffer[0].point;
                    Vector3 localSurface = _spawner.InverseTransformPoint(surfacePosition);
                    bool validPosition = true;
                    for (int i = 0; i < _clusters.Count; i++)
                    {
                        if (Vector3.Distance(localSurface, _clusters[i].center) < _settings.minDistanceBetweenClusters)
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
                            radius = (float)(_random.NextDouble() * (_settings.clusterRadiusRange.y - _settings.clusterRadiusRange.x) + _settings.clusterRadiusRange.x)
                        };
                        _clusters.Add(newCluster);
                        clustersCreated++;
                    }
                }
            }
        }

        public PlacementPoint GeneratePoint()
        {
            if (_clusters == null || _clusters.Count == 0)
                return new PlacementPoint { localPosition = Vector3.zero };

            int attempts = 0;
            const int maxAttempts = 20;
            while (attempts++ < maxAttempts)
            {
                int idx = _random.Next(_clusters.Count);
                ClusterData cluster = _clusters[idx];

                float angle = (float)(_random.NextDouble() * 2.0 * Mathf.PI);
                float radius = (float)(_random.NextDouble() * cluster.radius);
                Vector2 randomPoint = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Vector3 localPos = cluster.center + new Vector3(randomPoint.x, 0, randomPoint.y);

                float halfX = _settings.dimensions.x * 0.5f;
                float halfZ = _settings.dimensions.z * 0.5f;
                if (Mathf.Abs(localPos.x) > halfX || Mathf.Abs(localPos.z) > halfZ)
                    continue;

                return new PlacementPoint { localPosition = localPos };
            }

            return new PlacementPoint { localPosition = Vector3.zero };
        }

        public bool HasMorePoints() => _clusters != null && _clusters.Count > 0;
        public int GetClusterCount() => _clusters?.Count ?? 0;
        public List<ClusterData> GetClusters() => _clusters;
    }
}