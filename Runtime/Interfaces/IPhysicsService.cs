// IPhysicsService.cs
using UnityEngine;

namespace NeonImperium.WorldGeneration
{
    public interface IPhysicsService
    {
        bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask);
        int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask);
        bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask);
        int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask);
        bool CheckSphere(Vector3 position, float radius, int layerMask);
    }
}