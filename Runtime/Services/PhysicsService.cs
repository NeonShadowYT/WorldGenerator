// PhysicsService.cs
using UnityEngine;

namespace NeonImperium.WorldGeneration
{
    public class PhysicsService : IPhysicsService
    {
        public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return Physics.Raycast(origin, direction, out hitInfo, maxDistance, layerMask);
        }

        public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return Physics.RaycastNonAlloc(origin, direction, results, maxDistance, layerMask);
        }

        public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return Physics.SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask);
        }

        public int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return Physics.SphereCastNonAlloc(origin, radius, direction, results, maxDistance, layerMask);
        }

        public bool CheckSphere(Vector3 position, float radius, int layerMask)
        {
            return Physics.CheckSphere(position, radius, layerMask);
        }
    }
}