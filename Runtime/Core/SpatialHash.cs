using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_MATHEMATICS
using Unity.Mathematics;
#endif

namespace NeonImperium.WorldGenerations
{
    /// <summary>
    /// Пространственный хэш для проверки близости точек.
    /// Ключ ячейки — uint из math.hash(int3) (или ручной скрэмбл в фоллбэке).
    /// cellSize должен быть ≥ radius поиска.
    /// </summary>
    public sealed class SpatialHash
    {
        private struct Entry
        {
            public int index;
            public Vector3 position;
        }

        private struct Bucket
        {
            public Entry[] items;
            public int count;
        }

        private const int InitialBucketCapacity = 4;

        private readonly Dictionary<uint, Bucket> _buckets;
        private readonly float _invCellSize;

        public SpatialHash(float cellSize, int capacity)
        {
            float cs = cellSize > 0f ? cellSize : 1f;
            _invCellSize = 1f / cs;
            _buckets = new Dictionary<uint, Bucket>(capacity > 0 ? capacity : 16);
        }

        public void Clear() => _buckets.Clear();

        public void Insert(Vector3 pos, int index)
        {
            uint key = ComputeKey(pos);

            if (!_buckets.TryGetValue(key, out Bucket b))
            {
                b.items = new Entry[InitialBucketCapacity];
                b.count = 0;
            }
            else if (b.count == b.items.Length)
            {
                Array.Resize(ref b.items, b.items.Length * 2);
            }

            b.items[b.count].index = index;
            b.items[b.count].position = pos;
            b.count++;
            _buckets[key] = b;
        }

        /// <summary>Есть ли уже вставленная точка ближе, чем radius. Обход 3×3×3 соседних ячеек.</summary>
        public bool HasWithin(Vector3 pos, float radius)
        {
            float sqr = radius * radius;

#if UNITY_MATHEMATICS
            float3 fpos = new float3(pos.x, pos.y, pos.z);
            int3 cell = (int3)math.floor(fpos * _invCellSize);

            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            for (int dz = -1; dz <= 1; dz++)
            {
                uint key = math.hash(cell + new int3(dx, dy, dz));
                if (!_buckets.TryGetValue(key, out Bucket b)) continue;

                for (int i = 0; i < b.count; i++)
                {
                    float ddx = b.items[i].position.x - pos.x;
                    float ddy = b.items[i].position.y - pos.y;
                    float ddz = b.items[i].position.z - pos.z;
                    if (ddx * ddx + ddy * ddy + ddz * ddz < sqr) return true;
                }
            }
#else
            int cx = FloorToInt(pos.x * _invCellSize);
            int cy = FloorToInt(pos.y * _invCellSize);
            int cz = FloorToInt(pos.z * _invCellSize);

            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            for (int dz = -1; dz <= 1; dz++)
            {
                uint key = HashManual(cx + dx, cy + dy, cz + dz);
                if (!_buckets.TryGetValue(key, out Bucket b)) continue;

                for (int i = 0; i < b.count; i++)
                {
                    if ((b.items[i].position - pos).sqrMagnitude < sqr) return true;
                }
            }
#endif
            return false;
        }

        private uint ComputeKey(Vector3 pos)
        {
#if UNITY_MATHEMATICS
            float3 fpos = new float3(pos.x, pos.y, pos.z);
            int3 cell = (int3)math.floor(fpos * _invCellSize);
            return math.hash(cell);
#else
            int cx = FloorToInt(pos.x * _invCellSize);
            int cy = FloorToInt(pos.y * _invCellSize);
            int cz = FloorToInt(pos.z * _invCellSize);
            return HashManual(cx, cy, cz);
#endif
        }

        private static int FloorToInt(float v) => (int)Mathf.Floor(v);

        private static uint HashManual(int x, int y, int z)
        {
            unchecked
            {
                uint h = (uint)x * 73856093u;
                h ^= (uint)y * 19349663u;
                h ^= (uint)z * 83492791u;
                return h;
            }
        }
    }
}