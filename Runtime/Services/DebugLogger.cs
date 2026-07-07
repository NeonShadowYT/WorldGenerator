// DebugLogger.cs
using UnityEngine;
using System.Collections.Generic;

namespace NeonImperium.WorldGeneration
{
    public class DebugLogger : IDebugLogger
    {
        private List<DebugRay> debugRays = new List<DebugRay>();
        private DebugRaySettings settings;

        public DebugLogger(DebugRaySettings settings)
        {
            this.settings = settings;
        }

        public void AddDebugRay(Vector3 start, Vector3 direction, float distance, Color color, DebugRayType rayType)
        {
            if (!settings.enabled) return;
            bool shouldDraw = rayType switch
            {
                DebugRayType.Main => settings.showMainRays,
                DebugRayType.Stability => settings.showStabilityRays,
                DebugRayType.Floor => settings.showFloorRays,
                DebugRayType.Avoidance => settings.showAvoidanceRays,
                DebugRayType.Ceiling => settings.showCeilingRays,
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

        public void ClearDebugRays() => debugRays.Clear();

        public List<DebugRay> GetDebugRays() => debugRays;
    }
}