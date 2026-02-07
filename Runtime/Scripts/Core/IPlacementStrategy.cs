using UnityEngine;

namespace NeonImperium.WorldGeneration
{
    public interface IPlacementStrategy
    {
        PlacementPoint GeneratePoint(SpawnSettings settings, Transform spawner);
    }

    public class RandomPlacement : IPlacementStrategy
    {
        public PlacementPoint GeneratePoint(SpawnSettings settings, Transform spawner)
        {
            float halfX = settings.dimensions.x * 0.5f;
            float halfZ = settings.dimensions.z * 0.5f;
            
            return new PlacementPoint
            {
                localPosition = new Vector3(
                    Random.Range(-halfX, halfX),
                    0,
                    Random.Range(-halfZ, halfZ))
            };
        }
    }
}