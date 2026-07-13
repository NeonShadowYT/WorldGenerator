// IDebugLogger.cs
using UnityEngine;
using System.Collections.Generic;

namespace NeonImperium.WorldGenerations
{
    public interface IDebugLogger
    {
        void AddDebugRay(Vector3 start, Vector3 direction, float distance, Color color, DebugRayType rayType);
        void ClearDebugRays();
        List<DebugRay> GetDebugRays();
    }
}