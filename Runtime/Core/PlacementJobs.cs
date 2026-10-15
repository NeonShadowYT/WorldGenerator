using System;
using UnityEngine;

#if UNITY_2022_2_OR_NEWER
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
#endif

#if UNITY_MATHEMATICS
using Unity.Mathematics;
#endif

namespace NeonImperium.WorldGenerations
{
    /// <summary>
    /// Переиспользуемые persistent NativeArray для RaycastCommand.ScheduleBatch.
    /// Capacity растёт только вверх (power-of-2). Dispose обязателен.
    /// RaycastCommand.ScheduleBatch уже Burst-компилируется внутри Unity,
    /// поэтому собственный IJobParallelFor не нужен — это даёт максимальную
    /// производительность без управляемых аллокаций в горячем цикле.
    /// </summary>
    public sealed class PlacementJobs : IDisposable
    {
#if UNITY_2022_2_OR_NEWER
        private const int MinCapacity = 64;
        private const int InnerLoopBatchCount = 1;

        private NativeArray<RaycastCommand> _commands;
        private NativeArray<RaycastHit> _results;
        private int _capacity;
        private bool _disposed;

        public bool IsAvailable => !_disposed;
        public int Capacity => _capacity;

        /// <summary>Расширяет persistent-буферы (no-op, если capacity достаточна).</summary>
        public void EnsureCapacity(int size)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PlacementJobs));
            if (size <= _capacity) return;

            int newCap = Mathf.NextPowerOfTwo(Mathf.Max(size, MinCapacity));
            ReleaseBuffers();

            _commands = new NativeArray<RaycastCommand>(newCap, Allocator.Persistent);
            _results = new NativeArray<RaycastHit>(newCap, Allocator.Persistent);
            _capacity = newCap;
        }

        public void SetCommand(int index, RaycastCommand command) => _commands[index] = command;

#if UNITY_MATHEMATICS
        /// <summary>float3-обёртка для Burst-friendly API (RaycastCommand принимает Vector3 через implicit).</summary>
        public void SetCommand(int index, float3 origin, float3 direction, float distance, int layerMask)
        {
            QueryParameters qp = new QueryParameters { layerMask = layerMask };
            _commands[index] = new RaycastCommand(origin, direction, qp, distance);
        }
#endif

        public RaycastHit GetResult(int index) => _results[index];

        /// <summary>ScheduleBatch + Complete. count ≤ Capacity, иначе InvalidOperationException.</summary>
        public void Execute(int count, int commandsPerJob)
        {
            if (count <= 0) return;
            if (count > _capacity)
                throw new InvalidOperationException("EnsureCapacity must be called first.");

            int perJob = commandsPerJob < 1 ? 1 : (commandsPerJob > count ? count : commandsPerJob);
            RaycastCommand.ScheduleBatch(_commands, _results, perJob, InnerLoopBatchCount).Complete();
        }

        private void ReleaseBuffers()
        {
            if (_commands.IsCreated) _commands.Dispose();
            if (_results.IsCreated) _results.Dispose();
            _commands = default;
            _results = default;
            _capacity = 0;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ReleaseBuffers();
        }
#else
        public bool IsAvailable => false;
        public int Capacity => 0;
        public void EnsureCapacity(int size) { }
        public void SetCommand(int index, RaycastCommand command) { }
        public RaycastHit GetResult(int index) => default;
        public void Execute(int count, int commandsPerJob) { }
        public void Dispose() { }
#endif
    }
}