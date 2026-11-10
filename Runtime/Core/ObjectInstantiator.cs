using System.Collections.Generic;
using System.Threading;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NeonImperium.WorldGenerations
{
    /// <summary>
    /// Создание / переиспользование объектов из кэша размещений.
    ///
    /// ═══════════════════════════════════════════════════════════════════════════
    /// КОНТРАКТ ВЛАДЕНИЯ
    /// ═══════════════════════════════════════════════════════════════════════════
    /// При reuseExistingObjects = true все дочерние объекты spawner'а — его собственность.
    /// Spawner приводит их состав в точное соответствие с кэшем:
    ///   • matching     → reuse in-place (transform + extensions)
    ///   • excess       → Destroy (НЕ SetActive(false))
    ///   • missing      → Instantiate
    ///   • orphaned     → Destroy (префаб удалён из settings.prefabs)
    ///
    /// Исключение (editor-only): plain GameObject без prefab-связи — это ручное
    /// добавление пользователя. Не трогаем, warn один раз за сессию.
    ///
    /// ═══════════════════════════════════════════════════════════════════════════
    /// ИДЕНТИФИКАЦИЯ ИСТОЧНИКА ПРЕФАБА
    /// ═══════════════════════════════════════════════════════════════════════════
    /// • Editor (edit и play)  — PrefabUtility.GetCorrespondingObjectFromSource.
    ///                            Работает для любых prefab instances.
    /// • Runtime build         — PrefabSourceTag, добавляется при создании.
    ///
    /// ═══════════════════════════════════════════════════════════════════════════
    /// СТАБИЛЬНОСТЬ В ЛЮБОМ ИСХОДНОМ СОСТОЯНИИ СЦЕНЫ
    /// ═══════════════════════════════════════════════════════════════════════════
    /// Сцена может быть какой угодно: свежая, после старой версии плагина,
    /// с ручным спавном, с префабами из другого списка. После ОДНОЙ регенерации
    /// с reuse она конвергирует к чистому состоянию. Дальше — стабильна.
    ///
    /// ═══════════════════════════════════════════════════════════════════════════
    /// ОПТИМИЗАЦИЯ
    /// ═══════════════════════════════════════════════════════════════════════════
    /// Single prefab + чистый steady state → быстрый позиционный путь (без хэш-мапов).
    /// Multi prefab или загрязнённое состояние   → type-aware путь с группировкой.
    /// </summary>
    public static class ObjectInstantiator
    {
        private const int BatchSize = 20;

#if UNITY_EDITOR
        private static bool _manualEditorWarned;
#endif

        public static async Awaitable InstantiateBatchAsync(
            IReadOnlyList<PlacementData> cache, Transform parent,
            int population, List<WorldGenerationExtension> extensions,
            bool reuseExisting, IReadOnlyList<GameObject> knownPrefabs,
            CancellationToken token = default)
        {
            if (cache == null || parent == null) return;

            int count = cache.Count < population ? cache.Count : population;

            // ── Fresh spawn: полная очистка уже сделана вызывающим, просто создаём.
            if (!reuseExisting)
            {
                await InstantiateFreshAsync(cache, parent, count, extensions, token);
                return;
            }

            int prefabCount = knownPrefabs?.Count ?? 0;

            // ── Multi-prefab или вырожденный (0) — всегда type-aware.
            if (prefabCount != 1)
            {
                await ReuseByPrefabTypeAsync(cache, parent, count, extensions, token);
                return;
            }

            // ── Single prefab — быстрый путь с защитой от «чужих» детей.
            await ReusePositionalOrEscalateAsync(cache, parent, count, extensions,
                knownPrefabs[0], token);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // FRESH
        // ═══════════════════════════════════════════════════════════════════════════

        private static async Awaitable InstantiateFreshAsync(
            IReadOnlyList<PlacementData> cache, Transform parent, int count,
            List<WorldGenerationExtension> extensions, CancellationToken token)
        {
            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                token.ThrowIfCancellationRequested();

                PlacementData data = cache[i];
                if (!data.isValid || data.prefab == null) continue;

                GameObject obj = CreateObject(data.prefab, parent);
                ApplyTransform(obj, data);
                NotifyExtensions(obj, extensions);

                if (++spawned % BatchSize == 0)
                    await Awaitable.NextFrameAsync(token);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // SINGLE PREFAB — быстрый позиционный путь с эскалацией
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Steady state: все дети — ожидаемый префаб → позиционный reuse (index → transform).
        /// Загрязнённое состояние → эскалация в type-aware путь для разовой чистки.
        /// Следующий regen пойдёт быстрым путём.
        ///
        /// Excess детей — Destroy, НЕ SetActive(false). Никаких мертвецов в иерархии.
        /// </summary>
        private static async Awaitable ReusePositionalOrEscalateAsync(
            IReadOnlyList<PlacementData> cache, Transform parent, int count,
            List<WorldGenerationExtension> extensions, GameObject expectedPrefab,
            CancellationToken token)
        {
            int existingCount = parent.childCount;

            // ── Быстрый скан: есть ли среди детей префаб НЕ из ожидаемого?
            // null (untagged / plain GO) не считаем чужим: неизвестно, что это,
            // а Destroy чужого в позиционном пути недопустим.
            if (existingCount > 0 && expectedPrefab != null)
            {
                for (int i = 0; i < existingCount; i++)
                {
                    GameObject child = parent.GetChild(i).gameObject;
                    if (child == null) continue;

                    GameObject src = GetSourcePrefab(child);
                    if (src != null && src != expectedPrefab)
                    {
                        Debug.LogWarning(
                            $"[Reuse] {parent.name}: найден ребёнок от префаба '{src.name}', " +
                            $"ожидался '{expectedPrefab.name}'. Эскалирую в type-aware путь " +
                            $"для очистки. Следующие регенерации пойдут быстрым путём.");

                        await ReuseByPrefabTypeAsync(cache, parent, count, extensions, token);
                        return;
                    }
                }
            }

            // ── Быстрый путь: index-based in-place.
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
                    if (obj == null) { childIdx++; continue; }

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

            // ── Excess: Destroy. Не SetActive(false) — иначе мёртвый груз и
            //    рассинхрон SpawnedCount = transform.childCount.
            //    Обратный обход безопасен для DestroyImmediate (индексы < i не меняются).
            for (int i = existingCount - 1; i >= childIdx; i--)
            {
                token.ThrowIfCancellationRequested();
                GameObject obj = parent.GetChild(i).gameObject;
                if (obj != null) DestroyObject(obj);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // MULTI PREFAB — type-aware reuse
        // ═══════════════════════════════════════════════════════════════════════════

        private static async Awaitable ReuseByPrefabTypeAsync(
            IReadOnlyList<PlacementData> cache, Transform parent, int count,
            List<WorldGenerationExtension> extensions, CancellationToken token)
        {
            // ── 1. Что нужно: prefab → [cacheIndex...]
            var needed = new Dictionary<GameObject, List<int>>();
            for (int i = 0; i < count; i++)
            {
                PlacementData data = cache[i];
                if (!data.isValid || data.prefab == null) continue;

                if (!needed.TryGetValue(data.prefab, out var list))
                    needed[data.prefab] = list = new List<int>();
                list.Add(i);
            }

            // ── 2. Что стоит: prefab → [child...] + учёт бесхозных
            var existingByPrefab = new Dictionary<GameObject, List<GameObject>>();
            var legacyUntagged = new List<GameObject>();
#if UNITY_EDITOR
            var manualPlainGos = new List<GameObject>();
#endif

            int totalExisting = parent.childCount;
            for (int i = 0; i < totalExisting; i++)
            {
                GameObject obj = parent.GetChild(i).gameObject;
                if (obj == null) continue;

                GameObject source = GetSourcePrefab(obj);
                if (source == null)
                {
#if UNITY_EDITOR
                    // PrefabUtility вернул null → plain GO → ручное добавление.
                    manualPlainGos.Add(obj);
#else
                    // Runtime: старая сцена до PrefabSourceTag или ручной spawn.
                    legacyUntagged.Add(obj);
#endif
                    continue;
                }

                if (!existingByPrefab.TryGetValue(source, out var list))
                    existingByPrefab[source] = list = new List<GameObject>();
                list.Add(obj);
            }

            // ── 3. Одноразовая чистка legacy (runtime, с громким warn)
            if (legacyUntagged.Count > 0)
            {
                Debug.LogWarning(
                    $"[Reuse] {parent.name}: {legacyUntagged.Count} детей без PrefabSourceTag " +
                    $"(старая сцена до миграции, либо ручной спавн в runtime). " +
                    $"Удаляются один раз — с этого момента иерархия управляемая.");
                for (int i = 0; i < legacyUntagged.Count; i++)
                    DestroyObject(legacyUntagged[i]);
            }

#if UNITY_EDITOR
            if (manualPlainGos.Count > 0 && !_manualEditorWarned)
            {
                _manualEditorWarned = true;
                Debug.LogWarning(
                    $"[Reuse] {parent.name}: {manualPlainGos.Count} plain GameObject без " +
                    $"prefab-связи. Оставлены как ручные добавления (не удаляются).");
            }
#endif

            // ── 4. Разложить: reuse / delete / instantiate
            int processed = 0;
            var toDelete = new List<GameObject>();
            var toInstantiate = new List<(GameObject prefab, PlacementData data)>();

            foreach (var kv in needed)
            {
                GameObject prefab = kv.Key;
                List<int> indices = kv.Value;

                existingByPrefab.TryGetValue(prefab, out var existingList);
                int existCount = existingList?.Count ?? 0;
                int reuseCount = existCount < indices.Count ? existCount : indices.Count;

                // reuse на месте
                for (int i = 0; i < reuseCount; i++)
                {
                    token.ThrowIfCancellationRequested();
                    GameObject obj = existingList[i];
                    if (obj == null) continue;

                    if (!obj.activeSelf) obj.SetActive(true);
                    ApplyTransform(obj, cache[indices[i]]);
                    NotifyExtensions(obj, extensions);

                    if (++processed % BatchSize == 0)
                        await Awaitable.NextFrameAsync(token);
                }

                // excess → delete
                for (int i = reuseCount; i < existCount; i++)
                    if (existingList[i] != null) toDelete.Add(existingList[i]);

                // missing → instantiate
                for (int i = reuseCount; i < indices.Count; i++)
                    toInstantiate.Add((prefab, cache[indices[i]]));
            }

            // ── 5. Префабы, исчезнувшие из settings.prefabs → delete
            foreach (var kv in existingByPrefab)
            {
                if (needed.ContainsKey(kv.Key)) continue;
                for (int i = 0; i < kv.Value.Count; i++)
                    if (kv.Value[i] != null) toDelete.Add(kv.Value[i]);
            }

            // ── 6. Удаление excess
            for (int i = 0; i < toDelete.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                DestroyObject(toDelete[i]);
            }

            // ── 7. Инстанцирование недостающих
            for (int i = 0; i < toInstantiate.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                var (prefab, data) = toInstantiate[i];

                GameObject obj = CreateObject(prefab, parent);
                ApplyTransform(obj, data);
                NotifyExtensions(obj, extensions);

                if (++processed % BatchSize == 0)
                    await Awaitable.NextFrameAsync(token);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // PREFAB IDENTIFICATION
        // ═══════════════════════════════════════════════════════════════════════════

        private static GameObject GetSourcePrefab(GameObject obj)
        {
#if UNITY_EDITOR
            var editorSrc = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            if (editorSrc != null) return editorSrc;
#endif
            var tag = obj.GetComponent<PrefabSourceTag>();
            return tag != null ? tag.sourcePrefab : null;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // CREATION / DESTRUCTION
        // ═══════════════════════════════════════════════════════════════════════════

        private static GameObject CreateObject(GameObject prefab, Transform parent)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                var obj = Object.Instantiate(prefab, parent);
                AttachRuntimeTag(obj, prefab);
                return obj;
            }
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
#else
            var obj = Object.Instantiate(prefab, parent);
            AttachRuntimeTag(obj, prefab);
            return obj;
#endif
        }

        private static void AttachRuntimeTag(GameObject obj, GameObject sourcePrefab)
        {
            var tag = obj.GetComponent<PrefabSourceTag>();
            if (tag == null) tag = obj.AddComponent<PrefabSourceTag>();
            tag.sourcePrefab = sourcePrefab;
            tag.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
        }

        private static void DestroyObject(GameObject obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(obj);
                return;
            }
#endif
            Object.Destroy(obj);
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

    /// <summary>
    /// Runtime-маркер источника префаба. Только для собранной игры:
    /// в редакторе PrefabUtility определяет источник надёжнее. HideFlags
    /// не даёт тегу попасть в сериализацию сцены.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrefabSourceTag : MonoBehaviour
    {
        public GameObject sourcePrefab;
    }
}