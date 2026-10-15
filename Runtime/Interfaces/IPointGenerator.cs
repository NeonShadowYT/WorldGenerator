using UnityEngine;
using System.Collections.Generic;

namespace NeonImperium.WorldGenerations
{
    public interface IPointGenerator
    {
        void Initialize(SpawnSettings settings, Transform spawner, IDebugLogger debugLogger,
                        IPhysicsService physicsService, int seed, List<ISpawnZone> zones);
        PlacementPoint GeneratePoint();
        bool HasMorePoints();
        int GetClusterCount();
    }
}