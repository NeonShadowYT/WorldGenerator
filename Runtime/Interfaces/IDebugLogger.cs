using UnityEngine;
using System.Collections.Generic;

namespace NeonImperium.WorldGenerations
{
    public interface IDebugLogger
    {
        /// <summary>true, если отладочные лучи включены и будут записываться.</summary>
        bool Enabled { get; }

        void AddDebugRay(Vector3 start, Vector3 direction, float distance, Color color, DebugRayType rayType);
        void ClearDebugRays();
        List<DebugRay> GetDebugRays();
    }

    /// <summary>
    /// Null-object реализация IDebugLogger. Безопасна для вызовов без null-проверок.
    /// Используется как дефолт, когда логирование отключено или недоступно.
    /// </summary>
    public sealed class NullDebugLogger : IDebugLogger
    {
        public static readonly NullDebugLogger Instance = new();
        private static readonly List<DebugRay> EmptyRays = new(0);

        private NullDebugLogger() { }

        public bool Enabled => false;
        public void AddDebugRay(Vector3 start, Vector3 direction, float distance, Color color, DebugRayType rayType) { }
        public void ClearDebugRays() { }
        public List<DebugRay> GetDebugRays() => EmptyRays;
    }

    /// <summary>
    /// Extension-хелперы: убирают повторяющиеся `if (log != null && log.Enabled)`.
    /// </summary>
    public static class DebugLoggerExtensions
    {
        /// <summary>Добавляет луч, если logger != null и Enabled = true. Иначе — no-op.</summary>
        public static void TryAddRay(this IDebugLogger log, Vector3 start, Vector3 direction,
            float distance, Color color, DebugRayType rayType)
        {
            if (log != null && log.Enabled)
                log.AddDebugRay(start, direction, distance, color, rayType);
        }
    }
}