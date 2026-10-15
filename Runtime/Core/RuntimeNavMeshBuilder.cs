using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace NeonImperium.WorldGenerations
{
    /// <summary>
    /// Runtime-построение NavMesh через UnityEngine.AI.NavMeshBuilder.
    /// Не блокирует главный поток — UpdateNavMeshDataAsync.
    /// </summary>
    public static class RuntimeNavMeshBuilder
    {
        private static readonly List<NavMeshBuildMarkup> _markups = new(1);

        /// <summary>
        /// Собирает геометрию в bounds и запускает асинхронный bake в data.
        /// sources переиспользуется caller'ом (grow-only). Возвращает AsyncOperation или null.
        /// </summary>
        public static AsyncOperation RegenerateAsync(NavMeshData data, Bounds bounds,
            NavMeshCollectGeometry geometry, int layerMask, int agentTypeID,
            List<NavMeshBuildSource> sources)
        {
            if (data == null) return null;
            if (sources == null) return null;

            sources.Clear();
            _markups.Clear();

            NavMeshBuilder.CollectSources(bounds, layerMask, geometry, 0, _markups, sources);

            NavMeshBuildSettings buildSettings = NavMesh.GetSettingsByID(agentTypeID);

            // Если agentTypeID не найден — Unity возвращает дефолт; страхуемся на 0.
            if (buildSettings.agentTypeID != agentTypeID && agentTypeID != 0)
                buildSettings = NavMesh.GetSettingsByID(0);

            return NavMeshBuilder.UpdateNavMeshDataAsync(data, buildSettings, sources, bounds);
        }
    }
}