using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace NeonImperium.WorldGenerations
{
    public sealed class RandomPointGenerator : IPointGenerator
    {
        private FastRandom _rng;
        private List<ISpawnZone> _zones;

        public void Initialize(SpawnSettings settings, Transform spawner, IDebugLogger debugLogger,
                               IPhysicsService physicsService, int seed, List<ISpawnZone> zones)
        {
            _rng = new FastRandom(seed);
            _zones = zones;
        }

        public PlacementPoint GeneratePoint()
        {
            if (_zones == null || _zones.Count == 0)
                return new PlacementPoint { zoneIndex = -1 };

            int idx = _rng.Next(_zones.Count);
            Vector2 xz = _zones[idx].GetRandomXZ(_rng);
            return new PlacementPoint
            {
                localPosition = new Vector3(xz.x, 0f, xz.y),
                zoneIndex = idx
            };
        }

        public bool HasMorePoints() => true;
        public int GetClusterCount() => 0;
    }

    public sealed class ClusterPointGenerator : IPointGenerator
    {
        public sealed class ClusterData
        {
            public Vector3 center;
            public float radius;
            public int zoneIndex;
        }

        private const int CommandsPerJob = 64;

        private SpawnSettings _settings;
        private Transform _spawner;
        private List<ISpawnZone> _zones;
        private List<ClusterData> _clusters;
        private FastRandom _rng;

        public void Initialize(SpawnSettings settings, Transform spawner, IDebugLogger debugLogger,
                               IPhysicsService physicsService, int seed, List<ISpawnZone> zones)
        {
            _settings = settings;
            _spawner = spawner;
            _rng = new FastRandom(seed);
            _zones = zones;
            _clusters = new List<ClusterData>();
            GenerateClusters();
        }

        /// <summary>Батч RaycastCommand для поиска поверхностей под кандидатами кластеров.</summary>
        private void GenerateClusters()
        {
            _clusters.Clear();
            if (_zones == null || _zones.Count == 0) return;

            int targetClusters = Mathf.Min(_settings.clusterCount, _settings.population);
            if (targetClusters <= 0) return;

            int maxAttempts = _settings.clusterCount * 100;
            if (maxAttempts < targetClusters) maxAttempts = targetClusters;

            NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(maxAttempts, Allocator.TempJob);
            NativeArray<RaycastHit> results = new NativeArray<RaycastHit>(maxAttempts, Allocator.TempJob);
            int[] zoneIndices = new int[maxAttempts];

            try
            {
                QueryParameters queryParams = new QueryParameters { layerMask = _settings.collisionMask };

                for (int i = 0; i < maxAttempts; i++)
                {
                    int zoneIdx = _rng.Next(_zones.Count);
                    ISpawnZone zone = _zones[zoneIdx];
                    Vector2 xz = zone.GetRandomXZ(_rng);
                    Vector3 rayStart = _spawner.TransformPoint(new Vector3(xz.x, zone.RayStartY, xz.y));

                    commands[i] = new RaycastCommand(rayStart, Vector3.down, queryParams, zone.RayDistance);
                    zoneIndices[i] = zoneIdx;
                }

                RaycastCommand.ScheduleBatch(commands, results, CommandsPerJob, 1).Complete();

                float minClusterDist = _settings.minDistanceBetweenClusters;
                float minClusterDistSq = minClusterDist * minClusterDist;
                float rMin = _settings.clusterRadiusRange.x;
                float rRange = _settings.clusterRadiusRange.y - rMin;

                int clustersCreated = 0;
                for (int i = 0; i < maxAttempts && clustersCreated < targetClusters; i++)
                {
                    RaycastHit hit = results[i];
                    if (hit.collider == null) continue;

                    Vector3 localSurface = _spawner.InverseTransformPoint(hit.point);

                    bool valid = true;
                    for (int j = 0; j < _clusters.Count; j++)
                    {
                        if ((localSurface - _clusters[j].center).sqrMagnitude < minClusterDistSq)
                        {
                            valid = false;
                            break;
                        }
                    }
                    if (!valid) continue;

                    _clusters.Add(new ClusterData
                    {
                        center = localSurface,
                        radius = _rng.Range(rMin, rMin + rRange),
                        zoneIndex = zoneIndices[i]
                    });
                    clustersCreated++;
                }
            }
            finally
            {
                commands.Dispose();
                results.Dispose();
            }
        }

        /// <summary>Одна попытка. Ретраи делегированы внешнему циклу WorldGeneration.</summary>
        public PlacementPoint GeneratePoint()
        {
            if (_clusters == null || _clusters.Count == 0)
                return new PlacementPoint { zoneIndex = -1 };

            int idx = _rng.Next(_clusters.Count);
            ClusterData cluster = _clusters[idx];
            ISpawnZone zone = _zones[cluster.zoneIndex];

            float angle = _rng.Range(0f, 2f * Mathf.PI);
            float radius = _rng.Range(cluster.radius);
            float x = cluster.center.x + Mathf.Cos(angle) * radius;
            float z = cluster.center.z + Mathf.Sin(angle) * radius;

            if (!zone.ContainsXZ(x, z))
                return new PlacementPoint { zoneIndex = -1 };

            return new PlacementPoint
            {
                localPosition = new Vector3(x, 0f, z),
                zoneIndex = cluster.zoneIndex
            };
        }

        public bool HasMorePoints() => _clusters != null && _clusters.Count > 0;
        public int GetClusterCount() => _clusters?.Count ?? 0;
        public List<ClusterData> GetClusters() => _clusters;
    }
}