using UnityEngine;

namespace NeonImperium.WorldGeneration
{
    public interface IPointGenerator
    {
        void Initialize(SpawnSettings settings, Transform spawner, IDebugLogger debugLogger, IPhysicsService physicsService, int seed);
        PlacementPoint GeneratePoint();
        bool HasMorePoints();
        int GetClusterCount();
    }
}