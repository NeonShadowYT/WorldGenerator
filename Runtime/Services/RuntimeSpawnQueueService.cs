using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonImperium.WorldGenerations
{
    /// <summary>Очередь runtime-генерации с сортировкой по priority и последовательным выполнением.</summary>
    public static class RuntimeSpawnQueueService
    {
        private static readonly List<WorldGeneration> _queue = new();
        private static readonly Comparison<WorldGeneration> _priorityComparison = CompareByPriorityDesc;
        private static bool _processing;

        public static bool IsProcessing => _processing;
        public static int PendingCount => _queue.Count;

        public static void Enqueue(WorldGeneration spawner)
        {
            if (spawner == null || _queue.Contains(spawner)) return;
            _queue.Add(spawner);
            if (!_processing) _ = ProcessAsync();
        }

        /// <summary>Полная очистка очереди. Текущая генерация не прерывается.</summary>
        public static void Clear() => _queue.Clear();

        private static int CompareByPriorityDesc(WorldGeneration a, WorldGeneration b)
        {
            int pa = a != null ? a.settings.priority : 0;
            int pb = b != null ? b.settings.priority : 0;
            return pb.CompareTo(pa);
        }

        private static async Awaitable ProcessAsync()
        {
            if (_processing) return;
            _processing = true;

            try
            {
                while (_queue.Count > 0)
                {
                    _queue.Sort(_priorityComparison);
                    WorldGeneration spawner = _queue[0];
                    _queue.RemoveAt(0);

                    if (spawner == null || !spawner.enabled || !spawner.gameObject.activeInHierarchy)
                        continue;

                    if (!spawner.settings.autoGenerateOnStart || !spawner.settings.enableRuntimeGeneration)
                        continue;

                    try
                    {
                        await spawner.GenerateAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        // ожидаемая отмена — молча пропускаем
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, spawner);
                    }
                }
            }
            finally
            {
                _processing = false;
            }
        }
    }
}