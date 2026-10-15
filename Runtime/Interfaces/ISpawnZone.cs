using System.Collections.Generic;
using UnityEngine;

#if UNITY_MATHEMATICS
using Unity.Mathematics;
using Vec2 = Unity.Mathematics.float2;
#else
using Vec2 = UnityEngine.Vector2;
#endif

namespace NeonImperium.WorldGenerations
{
    /// <summary>Геометрический примитив зоны спавна. Все координаты — локальные координаты спавнера.</summary>
    public interface ISpawnZone
    {
        Bounds LocalBounds { get; }
        float RayStartY { get; }
        float RayDistance { get; }
        bool ContainsXZ(float x, float z);
        Vector2 GetRandomXZ(FastRandom rng);
        float GetSurfaceY(float x, float z, float fallbackY);
    }

    public sealed class BoxSpawnZone : ISpawnZone
    {
        private readonly Vector3 _center;
        private readonly Vector3 _halfSize;
        private readonly Bounds _bounds;

        public BoxSpawnZone(Vector3 center, Vector3 size)
        {
            _center = center;
            _halfSize = size * 0.5f;
            _bounds = new Bounds(center, size);
        }

        public Bounds LocalBounds => _bounds;
        public float RayStartY => _center.y + _halfSize.y;
        public float RayDistance => _halfSize.y * 2f;

        public bool ContainsXZ(float x, float z)
        {
            return x >= _center.x - _halfSize.x && x <= _center.x + _halfSize.x
                && z >= _center.z - _halfSize.z && z <= _center.z + _halfSize.z;
        }

        public float GetSurfaceY(float x, float z, float fallbackY) => _bounds.min.y;

        public Vector2 GetRandomXZ(FastRandom rng)
        {
            float x = rng.Range(-_halfSize.x, _halfSize.x) + _center.x;
            float z = rng.Range(-_halfSize.z, _halfSize.z) + _center.z;
            return new Vector2(x, z);
        }
    }

    /// <summary>
    /// Полигональная зона с 3D-объёмом: position вершины — центр вертикальной грани,
    /// VerticalSize — растягивает грань вверх и вниз.
    /// </summary>
    public sealed class PolygonSpawnZone : ISpawnZone
    {
        private const float RayBuffer = 5f;
        private const int RejectionAttempts = 16;
        private const float EpsilonSq = 1e-8f;
        private const float EpsilonLenSq = 1e-6f;

        private readonly Vec2[] _xz;
        private readonly float[] _vertYs;
        private readonly Vector3 _center;
        private readonly Bounds _bounds;
        private readonly float _rayStartY;
        private readonly float _rayDistance;
        private readonly float _minX;
        private readonly float _maxX;
        private readonly float _minZ;
        private readonly float _maxZ;
        private bool _clampedWarningLogged;

        public PolygonSpawnZone(Vector3 center, IList<PolygonVertex> vertices)
        {
            int n = vertices.Count;
            _xz = new Vec2[n];
            _vertYs = new float[n];
            _center = center;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            for (int i = 0; i < n; i++)
            {
                PolygonVertex v = vertices[i];
                Vector3 p = v.position;

                float rawSize = v.VerticalSize;
                float clamped = Mathf.Clamp(rawSize, PolygonVertex.MinVerticalSize, PolygonVertex.MaxVerticalSize);

                if (!Mathf.Approximately(rawSize, clamped) && !_clampedWarningLogged)
                {
                    Debug.LogWarning(
                        $"[PolygonSpawnZone] verticalSize {rawSize} clamped to {clamped} (center={center}).",
                        null);
                    _clampedWarningLogged = true;
                }

                float halfSize = clamped * 0.5f;
                float topY = p.y + halfSize;
                float botY = p.y - halfSize;

                _xz[i] = new Vec2(p.x, p.z);
                _vertYs[i] = p.y;

                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (botY < minY) minY = botY;
                if (topY > maxY) maxY = topY;
                if (p.z < minZ) minZ = p.z;
                if (p.z > maxZ) maxZ = p.z;
            }

            _minX = minX + center.x;
            _maxX = maxX + center.x;
            _minZ = minZ + center.z;
            _maxZ = maxZ + center.z;

            Vector3 bc = new(
                (minX + maxX) * 0.5f + center.x,
                (minY + maxY) * 0.5f + center.y,
                (minZ + maxZ) * 0.5f + center.z);
            Vector3 bs = new(
                Mathf.Max(0.01f, maxX - minX),
                Mathf.Max(0.01f, maxY - minY),
                Mathf.Max(0.01f, maxZ - minZ));
            _bounds = new Bounds(bc, bs);

            _rayStartY = center.y + maxY + RayBuffer;
            _rayDistance = (maxY - minY) + 2f * RayBuffer;
        }

        public Bounds LocalBounds => _bounds;
        public float RayStartY => _rayStartY;
        public float RayDistance => _rayDistance;

        /// <summary>Crossing-number на float2, early-out по AABB.</summary>
        public bool ContainsXZ(float x, float z)
        {
            if (x < _minX || x > _maxX || z < _minZ || z > _maxZ) return false;

            float lx = x - _center.x;
            float lz = z - _center.z;

#if UNITY_MATHEMATICS
            float2 p = new float2(lx, lz);
            bool inside = false;
            int n = _xz.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float2 pi = _xz[i];
                float2 pj = _xz[j];
                if ((pi.y > p.y) != (pj.y > p.y) &&
                    p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x)
                {
                    inside = !inside;
                }
            }
            return inside;
#else
            bool inside = false;
            int n = _xz.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Vec2 pi = _xz[i];
                Vec2 pj = _xz[j];
                if ((pi.y > lz) != (pj.y > lz) &&
                    lx < (pj.x - pi.x) * (lz - pi.y) / (pj.y - pi.y) + pi.x)
                {
                    inside = !inside;
                }
            }
            return inside;
#endif
        }

        public float GetSurfaceY(float x, float z, float fallbackY)
        {
            if (!ContainsXZ(x, z)) return fallbackY;

            float lx = x - _center.x;
            float lz = z - _center.z;
            float weightSum = 0f;
            float ySum = 0f;

            int n = _xz.Length;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vec2 a = _xz[i];
                Vec2 b = _xz[j];
                float abx = b.x - a.x;
                float aby = b.y - a.y;
                float abLenSq = abx * abx + aby * aby;
                if (abLenSq < EpsilonLenSq) continue;

                float apx = lx - a.x;
                float apy = lz - a.y;
                float t = Mathf.Clamp01((apx * abx + apy * aby) / abLenSq);

                float projX = a.x + abx * t;
                float projZ = a.y + aby * t;
                float dx = lx - projX;
                float dz = lz - projZ;
                float distSq = dx * dx + dz * dz;

                float edgeY = Mathf.Lerp(_vertYs[i], _vertYs[j], t);

                if (distSq < EpsilonSq) return edgeY + _center.y;

                float w = 1f / distSq;
                weightSum += w;
                ySum += w * edgeY;
            }

            if (weightSum < EpsilonSq) return fallbackY;
            return ySum / weightSum + _center.y;
        }

        public Vector2 GetRandomXZ(FastRandom rng)
        {
            Bounds b = _bounds;
            float minX = b.min.x;
            float minZ = b.min.z;
            float sz = b.size.x;
            float szZ = b.size.z;

            for (int i = 0; i < RejectionAttempts; i++)
            {
                float x = rng.Range(sz) + minX;
                float z = rng.Range(szZ) + minZ;
                if (ContainsXZ(x, z)) return new Vector2(x, z);
            }
            return new Vector2(_center.x, _center.z);
        }
    }

    public static class SpawnZoneBuilder
    {
        /// <summary>Строит runtime-список зон. Первой всегда идёт основная box-зона.</summary>
        public static List<ISpawnZone> Build(SpawnSettings settings)
        {
            List<ISpawnZone> zones = new(1 + (settings.extraBoxes?.Count ?? 0));

            if (settings.zoneMode == SpawnZoneMode.Box)
            {
                zones.Add(new BoxSpawnZone(Vector3.zero, settings.dimensions));

                List<BoxSpawnZoneSettings> extra = settings.extraBoxes;
                if (extra != null)
                {
                    for (int i = 0; i < extra.Count; i++)
                    {
                        BoxSpawnZoneSettings b = extra[i];
                        if (b == null || !b.enabled) continue;
                        zones.Add(new BoxSpawnZone(b.center, b.size));
                    }
                }
            }
            else
            {
                List<PolygonSpawnZoneSettings> polys = settings.polygons;
                if (polys != null)
                {
                    for (int i = 0; i < polys.Count; i++)
                    {
                        PolygonSpawnZoneSettings p = polys[i];
                        if (p == null || !p.enabled || p.vertices == null || p.vertices.Count < 3) continue;
                        zones.Add(new PolygonSpawnZone(p.center, p.vertices));
                    }
                }
            }

            if (zones.Count == 0)
                zones.Add(new BoxSpawnZone(Vector3.zero, settings.dimensions));

            return zones;
        }
    }
}