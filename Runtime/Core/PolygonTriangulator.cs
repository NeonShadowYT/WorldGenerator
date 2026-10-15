using System;
using UnityEngine;

namespace NeonImperium.WorldGenerations
{
    /// <summary>
    /// Ear-clipping триангуляция полигона в проекции XZ. O(n²), рассчитан на n ≤ 32.
    /// Не выпускает Span'ы наружу, не аллоцирует при n ≤ 64 (stackalloc).
    /// </summary>
    public static class PolygonTriangulator
    {
        private const float CrossEpsilon = 1e-6f;
        private const int StackAllocThreshold = 64;

        /// <summary>
        /// Триангулирует полигон в плоскости XZ. Возвращает количество треугольников.
        /// outputIndices должен вмещать (n − 2) * 3 индексов.
        /// </summary>
        public static int Triangulate(ReadOnlySpan<Vector3> vertices, Span<int> outputIndices)
        {
            int n = vertices.Length;
            if (n < 3) return 0;
            int maxTris = n - 2;
            if (outputIndices.Length < maxTris * 3) return 0;

            Span<int> remaining = n <= StackAllocThreshold ? stackalloc int[n] : new int[n];
            for (int i = 0; i < n; i++) remaining[i] = i;
            int count = n;

            // Ориентация: signed area в XZ.
            float signedArea = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector3 a = vertices[i];
                Vector3 b = vertices[(i + 1) % n];
                signedArea += a.x * b.z - b.x * a.z;
            }
            bool ccw = signedArea > 0f;

            int triCount = 0;
            int safety = n * n;

            while (count > 3 && safety-- > 0)
            {
                bool earCut = false;

                for (int i = 0; i < count; i++)
                {
                    int prevSlot = (i - 1 + count) % count;
                    int nextSlot = (i + 1) % count;
                    int ia = remaining[prevSlot];
                    int ib = remaining[i];
                    int ic = remaining[nextSlot];

                    Vector3 a = vertices[ia];
                    Vector3 b = vertices[ib];
                    Vector3 c = vertices[ic];

                    float cross = (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
                    if (ccw ? cross <= CrossEpsilon : cross >= -CrossEpsilon) continue;

                    bool containsVertex = false;
                    for (int j = 0; j < count; j++)
                    {
                        if (j == prevSlot || j == i || j == nextSlot) continue;
                        if (PointInTriangle2D(vertices[remaining[j]], a, b, c))
                        {
                            containsVertex = true;
                            break;
                        }
                    }
                    if (containsVertex) continue;

                    outputIndices[triCount * 3] = ia;
                    outputIndices[triCount * 3 + 1] = ib;
                    outputIndices[triCount * 3 + 2] = ic;
                    triCount++;

                    for (int k = i; k < count - 1; k++) remaining[k] = remaining[k + 1];
                    count--;
                    earCut = true;
                    break;
                }

                if (!earCut) break; // самопересечение / дегенерат — стоп без зацикливания
            }

            if (count == 3)
            {
                outputIndices[triCount * 3] = remaining[0];
                outputIndices[triCount * 3 + 1] = remaining[1];
                outputIndices[triCount * 3 + 2] = remaining[2];
                triCount++;
            }

            return triCount;
        }

        /// <summary>Проверяет выпуклость полигона в проекции XZ по знаку cross-product последовательных рёбер.</summary>
        public static bool IsConvex(ReadOnlySpan<Vector3> xzProjected)
        {
            int n = xzProjected.Length;
            if (n < 3) return false;

            bool hasPos = false;
            bool hasNeg = false;

            for (int i = 0; i < n; i++)
            {
                Vector3 a = xzProjected[i];
                Vector3 b = xzProjected[(i + 1) % n];
                Vector3 c = xzProjected[(i + 2) % n];

                float cross = (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
                if (cross > CrossEpsilon) hasPos = true;
                else if (cross < -CrossEpsilon) hasNeg = true;

                if (hasPos && hasNeg) return false;
            }
            return true;
        }

        private static bool PointInTriangle2D(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            float d1 = (p.x - b.x) * (a.z - b.z) - (a.x - b.x) * (p.z - b.z);
            float d2 = (p.x - c.x) * (b.z - c.z) - (b.x - c.x) * (p.z - c.z);
            float d3 = (p.x - a.x) * (c.z - a.z) - (c.x - a.x) * (p.z - a.z);

            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;

            return !(hasNeg && hasPos);
        }
    }
}