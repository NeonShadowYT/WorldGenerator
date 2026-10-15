using System.Collections.Generic;
using System.Threading;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NeonImperium.WorldGenerations
{
    /// <summary>Создание объектов из кэша размещений с батчингом по кадрам и опциональным переиспользованием.</summary>
    public static class ObjectInstantiator
    {
        private const int BatchSize = 20;

        public static async Awaitable InstantiateBatchAsync(
            IReadOnlyList<PlacementData> cache, Transform parent,
            int population, List<WorldGenerationExtension> extensions,
            bool reuseExisting, CancellationToken token = default)
        {
            if (cache == null || parent == null) return;

            int count = cache.Count < population ? cache.Count : population;

            if (reuseExisting)
            {
                await ReuseExistingAsync(cache, parent, count, extensions, token);
                return;
            }

            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                token.ThrowIfCancellationRequested();

                PlacementData data = cache[i];
                if (!data.isValid || data.prefab == null) continue;

                GameObject newObj = CreateObject(data.prefab, parent);
                ApplyTransform(newObj, data);
                NotifyExtensions(newObj, extensions);

                if (++spawned % BatchSize == 0)
                    await Awaitable.NextFrameAsync(token);
            }
        }

        private static async Awaitable ReuseExistingAsync(
            IReadOnlyList<PlacementData> cache, Transform parent,
            int count, List<WorldGenerationExtension> extensions, CancellationToken token)
        {
            int existingCount = parent.childCount;
            int childIdx = 0;
            int processed = 0;

            for (int i = 0; i < count; i++)
            {
                token.ThrowIfCancellationRequested();

                PlacementData data = cache[i];
                if (!data.isValid || data.prefab == null) continue;

                if (childIdx < existingCount)
                {
                    GameObject obj = parent.GetChild(childIdx).gameObject;
                    if (!obj.activeSelf) obj.SetActive(true);
                    ApplyTransform(obj, data);
                    NotifyExtensions(obj, extensions);
                    childIdx++;
                }
                else
                {
                    GameObject newObj = CreateObject(data.prefab, parent);
                    ApplyTransform(newObj, data);
                    NotifyExtensions(newObj, extensions);
                }

                if (++processed % BatchSize == 0)
                    await Awaitable.NextFrameAsync(token);
            }

            for (int i = childIdx; i < existingCount; i++)
            {
                GameObject obj = parent.GetChild(i).gameObject;
                if (obj.activeSelf) obj.SetActive(false);
            }
        }

        private static GameObject CreateObject(GameObject prefab, Transform parent)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return Object.Instantiate(prefab, parent);
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
#else
            return Object.Instantiate(prefab, parent);
#endif
        }

        private static void ApplyTransform(GameObject obj, in PlacementData data)
        {
            obj.transform.SetPositionAndRotation(data.position, data.rotation);
            obj.transform.localScale = data.scale;
        }

        private static void NotifyExtensions(GameObject obj, List<WorldGenerationExtension> extensions)
        {
            if (extensions == null) return;
            for (int i = 0; i < extensions.Count; i++)
            {
                WorldGenerationExtension e = extensions[i];
                if (e != null) e.OnGameObjectSpawned(obj);
            }
        }
    }
}