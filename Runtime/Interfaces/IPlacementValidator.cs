using UnityEngine;
using System.Collections.Generic;

namespace NeonImperium.WorldGeneration
{
    public interface IPlacementValidator
    {
        PlacementData Validate(PlacementPoint point, SpawnSettings settings, Transform spawner, List<PlacementData> existingPoints, IDebugLogger debugLogger, IPhysicsService physicsService);
    }
}