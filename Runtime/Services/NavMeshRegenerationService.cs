using UnityEngine;
using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NeonImperium.WorldGenerations
{
    /// <summary>
    /// Работа с NavMeshSurface через reflection — без жёсткой зависимости от пакета AI Navigation.
    /// HasNavMeshSurface / RebuildAllSurfacesSync доступны и в runtime, и в редакторе.
    /// </summary>
    public static class NavMeshRegenerationService
    {
        private const string SurfaceTypeName = "Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation";

        private static Type _surfaceType;
        private static MethodInfo _buildMethod;
        private static bool _typeResolved;

        public static bool HasNavMeshSurface()
        {
            EnsureResolved();
            return _surfaceType != null && _buildMethod != null;
        }

        private static void EnsureResolved()
        {
            if (_typeResolved) return;
            _typeResolved = true;

            Type t = Type.GetType(SurfaceTypeName, throwOnError: false);
            if (t == null) return;

            MethodInfo m = t.GetMethod("BuildNavMesh", BindingFlags.Public | BindingFlags.Instance);
            if (m == null) return;

            _surfaceType = t;
            _buildMethod = m;
        }

        /// <summary>
        /// Синхронно перестраивает все NavMeshSurface на активной сцене.
        /// Работает и в runtime, и в редакторе. Возвращает число успешно перестроенных surface'ов.
        /// </summary>
        public static int RebuildAllSurfacesSync()
        {
            if (!HasNavMeshSurface()) return 0;

            UnityEngine.Object[] surfaces = UnityEngine.Object.FindObjectsByType(
                _surfaceType, FindObjectsInactive.Include);
            if (surfaces == null || surfaces.Length == 0) return 0;

            int built = 0;
            for (int i = 0; i < surfaces.Length; i++)
            {
                try
                {
                    _buildMethod.Invoke(surfaces[i], null);
                    built++;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"NavMeshSurface [{surfaces[i].name}]: {e.Message}");
                }
            }
            return built;
        }

#if UNITY_EDITOR
        /// <summary>Editor: перегенерирует все NavMeshSurface + логирует результат.</summary>
        public static void RegenerateAll()
        {
            if (!HasNavMeshSurface())
            {
                Debug.LogWarning("NavMeshSurface не найден (пакет AI Navigation не установлен).");
                return;
            }

            int count = RebuildAllSurfacesSync();
            if (count > 0)
                Debug.Log($"🧭 NavMesh перегенерирован: {count} surface(ов).");
        }
#endif
    }
}