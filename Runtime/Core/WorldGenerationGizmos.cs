#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace NeonImperium.WorldGenerations
{
    /// <summary>Gizmos-отрисовка зон спавна, отладочных лучей и кластеров.</summary>
    public static class WorldGenerationGizmos
    {
        public static void Draw(SpawnSettings settings, Transform transform,
            IDebugLogger debugLogger, IPointGenerator pointGenerator)
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            Color baseColor = settings.gizmoColor;
            Color fill = new(baseColor.r, baseColor.g, baseColor.b, 0.10f);
            Color fillBot = new(baseColor.r, baseColor.g, baseColor.b, 0.05f);

            if (settings.zoneMode == SpawnZoneMode.Box)
            {
                Gizmos.color = baseColor;
                Gizmos.DrawWireCube(Vector3.zero, settings.dimensions);
                Gizmos.color = fill;
                Gizmos.DrawCube(Vector3.zero, settings.dimensions);

                if (settings.extraBoxes != null)
                {
                    for (int i = 0; i < settings.extraBoxes.Count; i++)
                    {
                        BoxSpawnZoneSettings b = settings.extraBoxes[i];
                        if (b == null || !b.enabled) continue;
                        Gizmos.color = baseColor;
                        Gizmos.DrawWireCube(b.center, b.size);
                        Gizmos.color = fill;
                        Gizmos.DrawCube(b.center, b.size);
                    }
                }
            }
            else if (settings.polygons != null)
            {
                for (int i = 0; i < settings.polygons.Count; i++)
                {
                    PolygonSpawnZoneSettings p = settings.polygons[i];
                    if (p == null || !p.enabled || p.vertices == null || p.vertices.Count < 3) continue;
                    DrawPolygon(p, baseColor, fill, fillBot);
                }
            }

            Gizmos.matrix = Matrix4x4.identity;

            if (settings.debugRaySettings.enabled && debugLogger != null)
            {
                List<DebugRay> rays = debugLogger.GetDebugRays();
                for (int i = 0; i < rays.Count; i++)
                {
                    Gizmos.color = rays[i].color;
                    Gizmos.DrawLine(rays[i].start, rays[i].end);
                }
            }

            if (settings.useClustering && pointGenerator is ClusterPointGenerator cpg)
            {
                List<ClusterPointGenerator.ClusterData> clusters = cpg.GetClusters();
                if (clusters != null)
                {
                    for (int i = 0; i < clusters.Count; i++)
                    {
                        ClusterPointGenerator.ClusterData c = clusters[i];
                        Vector3 worldCenter = transform.TransformPoint(c.center);
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(worldCenter, 0.5f);
                        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                        Gizmos.DrawSphere(worldCenter, c.radius);
                    }
                }
            }
        }

        private static void DrawPolygon(PolygonSpawnZoneSettings p, Color edge, Color fillTop, Color fillBot)
        {
            int n = p.vertices.Count;
            Vector3[] top = new Vector3[n];
            Vector3[] bot = new Vector3[n];

            for (int i = 0; i < n; i++)
            {
                PolygonVertex v = p.vertices[i];
                float half = v.VerticalSize * 0.5f;
                Vector3 c = p.center + v.position;
                top[i] = new Vector3(c.x, c.y + half, c.z);
                bot[i] = new Vector3(c.x, c.y - half, c.z);
            }

            Gizmos.color = fillTop; DrawFan(top, n);
            Gizmos.color = fillBot; DrawFan(bot, n);

            Gizmos.color = edge;
            for (int i = 0; i < n; i++)
            {
                Gizmos.DrawLine(top[i], top[(i + 1) % n]);
                Gizmos.DrawLine(bot[i], bot[(i + 1) % n]);
            }

            Color side = new(edge.r, edge.g, edge.b, 0.55f);
            Gizmos.color = side;
            for (int i = 0; i < n; i++) Gizmos.DrawLine(top[i], bot[i]);
        }

        private static void DrawFan(Vector3[] pts, int n)
        {
            Vector3 c = Vector3.zero;
            for (int i = 0; i < n; i++) c += pts[i];
            c /= n;

            for (int i = 0; i < n; i++)
            {
                Gizmos.DrawLine(c, pts[i]);
                Gizmos.DrawLine(c, pts[(i + 1) % n]);
            }
        }
    }
}
#endif