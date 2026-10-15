#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NeonImperium.WorldGenerations
{
    public static class SpawnZoneSceneEditor
    {
        private const float VertexSizeMul = 0.12f;
        private const float CenterSizeMul = 0.16f;
        private const float SizeHandleMul = 0.10f;
        private const float InsertSizeMul = 0.08f;
        private const float PickRadiusPx = 15f;
        private const int MaxTriangulatedVerts = 64;
        private const string SnapStepPrefKey = "NeonImperium.ZoneSnapStep";

        private static readonly Color EdgeColor = new(0.20f, 0.85f, 1f, 1f);
        private static readonly Color EdgeDisabled = new(0.50f, 0.50f, 0.50f, 0.35f);
        private static readonly Color FillColor = new(0.20f, 0.85f, 1f, 0.12f);
        private static readonly Color FillColorBottom = new(0.20f, 0.85f, 1f, 0.06f);
        private static readonly Color SideColor = new(0.20f, 0.85f, 1f, 0.55f);
        private static readonly Color VertexColor = new(1f, 0.85f, 0.15f);
        private static readonly Color VertexDisabled = new(0.55f, 0.55f, 0.55f);
        private static readonly Color FirstVertexColor = new(1f, 0.55f, 0.85f);
        private static readonly Color CenterColor = new(1f, 0.45f, 0.10f);
        private static readonly Color SizeHandleColor = new(0.30f, 0.85f, 1f);
        private static readonly Color InsertColor = new(0.25f, 1f, 0.35f);

        private static readonly int[] _triIndexBuffer = new int[(MaxTriangulatedVerts - 2) * 3];
        private static readonly Vector3[] _triRenderBuffer = new Vector3[3];

        private static Vector3[] _topBuffer = new Vector3[64];
        private static Vector3[] _botBuffer = new Vector3[64];

        private static float _snapStepCache = -1f;
        private static EntityId _activeSpawnerEntityId;

        /// <summary>Шаг снапа в метрах. 0 = отключено. Кэшируется в статик-поле.</summary>
        public static float SnapStep
        {
            get
            {
                if (_snapStepCache < 0f)
                    _snapStepCache = EditorPrefs.GetFloat(SnapStepPrefKey, 1f);
                return _snapStepCache;
            }
            set
            {
                _snapStepCache = value;
                EditorPrefs.SetFloat(SnapStepPrefKey, value);
            }
        }

        public static void DrawForSpawner(WorldGeneration spawner)
        {
            if (spawner == null) return;

            if (Event.current.type == EventType.MouseUp)
                _activeSpawnerEntityId = EntityId.None;

            if (GUIUtility.hotControl == 0)
                _activeSpawnerEntityId = EntityId.None;

            if (GUIUtility.hotControl != 0
                && !_activeSpawnerEntityId.Equals(EntityId.None)
                && !_activeSpawnerEntityId.Equals(spawner.GetEntityId()))
                return;

            if (spawner.settings.zoneMode == SpawnZoneMode.Box)
                DrawBoxes(spawner);
            else
                DrawPolygons(spawner);

            if (GUIUtility.hotControl != 0 && _activeSpawnerEntityId.Equals(EntityId.None))
                _activeSpawnerEntityId = spawner.GetEntityId();
        }

        // ─────────────── Snapping ───────────────

        private static Vector3 SnapVec(Vector3 v, float step)
        {
            if (step <= 0f) return v;
            return new Vector3(
                Mathf.Round(v.x / step) * step,
                Mathf.Round(v.y / step) * step,
                Mathf.Round(v.z / step) * step);
        }

        private static bool IsSnapActive() =>
            SnapStep > 0f && (Event.current.control || Event.current.command);

        // ─────────────── Undo ───────────────

        private static int BeginUndoGroup(UnityEngine.Object target, string name)
        {
            Undo.SetCurrentGroupName(name);
            int group = Undo.GetCurrentGroup();
            Undo.RecordObject(target, name);
            return group;
        }

        private static void EndUndoGroup(int group)
        {
            Undo.CollapseUndoOperations(group);
        }

        // ─────────────── Box ───────────────

        private static void DrawBoxes(WorldGeneration spawner)
        {
            List<BoxSpawnZoneSettings> boxes = spawner.settings.extraBoxes;
            if (boxes == null) return;

            Transform tr = spawner.transform;

            for (int i = 0; i < boxes.Count; i++)
            {
                BoxSpawnZoneSettings box = boxes[i];
                if (box == null || !box.enabled) continue;

                Vector3 worldCenter = tr.TransformPoint(box.center);

                Handles.color = EdgeColor;
                Handles.DrawWireCube(worldCenter, box.size);

                float size = HandleUtility.GetHandleSize(worldCenter) * CenterSizeMul;
                Handles.color = CenterColor;

                EditorGUI.BeginChangeCheck();
                Vector3 nw = Handles.FreeMoveHandle(worldCenter, size, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    if (IsSnapActive()) nw = SnapVec(nw, SnapStep);
                    int g = BeginUndoGroup(spawner, "Move Box Zone");
                    box.center = tr.InverseTransformPoint(nw);
                    EditorUtility.SetDirty(spawner);
                    EndUndoGroup(g);
                    SceneView.RepaintAll();
                }
            }
        }

        // ─────────────── Polygon ───────────────

        private static void DrawPolygons(WorldGeneration spawner)
        {
            List<PolygonSpawnZoneSettings> polys = spawner.settings.polygons;
            if (polys == null) return;

            Transform tr = spawner.transform;

            for (int i = 0; i < polys.Count; i++)
            {
                PolygonSpawnZoneSettings poly = polys[i];
                if (poly == null || poly.vertices == null || poly.vertices.Count == 0) continue;
                DrawPolygon(spawner, tr, poly);
            }
        }

        private static void EnsureVertexBuffers(int n)
        {
            if (_topBuffer.Length >= n) return;
            int newSize = Mathf.NextPowerOfTwo(n);
            _topBuffer = new Vector3[newSize];
            _botBuffer = new Vector3[newSize];
        }

        private static void DrawPolygon(WorldGeneration spawner, Transform tr, PolygonSpawnZoneSettings poly)
        {
            List<PolygonVertex> verts = poly.vertices;
            int n = verts.Count;

            Color edge = poly.enabled ? EdgeColor : EdgeDisabled;
            Color vertex = poly.enabled ? VertexColor : VertexDisabled;

            EnsureVertexBuffers(n);
            Vector3[] top = _topBuffer;
            Vector3[] bot = _botBuffer;

            for (int i = 0; i < n; i++)
            {
                PolygonVertex v = verts[i];
                float half = v.VerticalSize * 0.5f;
                Vector3 c = poly.center + v.position;
                top[i] = tr.TransformPoint(new Vector3(c.x, c.y + half, c.z));
                bot[i] = tr.TransformPoint(new Vector3(c.x, c.y - half, c.z));
            }

            if (poly.enabled && n >= 3)
            {
                Handles.color = FillColor;
                FillPolygonFan(new ReadOnlySpan<Vector3>(top, 0, n));
                Handles.color = FillColorBottom;
                FillPolygonFan(new ReadOnlySpan<Vector3>(bot, 0, n));
            }

            Handles.color = edge;
            for (int i = 0; i < n; i++)
            {
                Handles.DrawAAPolyLine(3.5f, top[i], top[(i + 1) % n]);
                Handles.DrawAAPolyLine(2f, bot[i], bot[(i + 1) % n]);
            }

            if (poly.enabled)
            {
                Handles.color = SideColor;
                for (int i = 0; i < n; i++)
                    Handles.DrawAAPolyLine(2f, top[i], bot[i]);
            }

            if (n < 3) return;
            if (!poly.enabled) return;

            Event ev = Event.current;

            if (n > 3 && ev.type == EventType.MouseDown && ev.button == 0 && ev.shift)
            {
                for (int i = 0; i < n; i++)
                {
                    Vector3 w = tr.TransformPoint(poly.center + verts[i].position);
                    float d = HandleUtility.DistanceToCircle(w, HandleUtility.GetHandleSize(w) * VertexSizeMul);
                    if (d < PickRadiusPx)
                    {
                        int g = BeginUndoGroup(spawner, "Delete Polygon Vertex");
                        verts.RemoveAt(i);
                        EditorUtility.SetDirty(spawner);
                        EndUndoGroup(g);
                        ev.Use();
                        SceneView.RepaintAll();
                        return;
                    }
                }
            }

            for (int i = 0; i < n; i++)
            {
                PolygonVertex a = verts[i];
                PolygonVertex b = verts[(i + 1) % n];
                Vector3 midPos = (a.position + b.position) * 0.5f;
                float midSize = (a.VerticalSize + b.VerticalSize) * 0.5f;
                Vector3 wMid = tr.TransformPoint(poly.center + midPos);

                float btnSize = HandleUtility.GetHandleSize(wMid) * InsertSizeMul;
                Handles.color = InsertColor;

                if (Handles.Button(wMid, Quaternion.identity, btnSize, btnSize, Handles.DotHandleCap))
                {
                    int g = BeginUndoGroup(spawner, "Insert Polygon Vertex");
                    verts.Insert(i + 1, new PolygonVertex(midPos, midSize));
                    EditorUtility.SetDirty(spawner);
                    EndUndoGroup(g);
                    SceneView.RepaintAll();
                    return;
                }
            }

            {
                Vector3 wCenter = tr.TransformPoint(poly.center);
                float size = HandleUtility.GetHandleSize(wCenter) * CenterSizeMul;
                Handles.color = CenterColor;

                EditorGUI.BeginChangeCheck();
                Vector3 nw = Handles.FreeMoveHandle(wCenter, size, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    if (IsSnapActive()) nw = SnapVec(nw, SnapStep);
                    int g = BeginUndoGroup(spawner, "Move Polygon Zone");
                    poly.center = tr.InverseTransformPoint(nw);
                    EditorUtility.SetDirty(spawner);
                    EndUndoGroup(g);
                    SceneView.RepaintAll();
                }
            }

            for (int i = 0; i < n; i++)
            {
                PolygonVertex v = verts[i];
                Vector3 c = poly.center + v.position;
                Vector3 topLocal = new(c.x, c.y + v.VerticalSize * 0.5f, c.z);
                Vector3 wTop = tr.TransformPoint(topLocal);

                float hs = HandleUtility.GetHandleSize(wTop) * SizeHandleMul;
                Handles.color = SizeHandleColor;

                EditorGUI.BeginChangeCheck();
                Vector3 nw = Handles.Slider(wTop, Vector3.up, hs, Handles.SphereHandleCap, 0f);
                if (EditorGUI.EndChangeCheck())
                {
                    if (IsSnapActive()) nw = SnapVec(nw, SnapStep);
                    Vector3 newTopLocal = tr.InverseTransformPoint(nw);
                    float newHalf = Mathf.Max(0.05f, Mathf.Abs(newTopLocal.y - c.y));
                    int g = BeginUndoGroup(spawner, "Resize Vertex Vertical Size");
                    v.VerticalSize = newHalf * 2f;
                    EditorUtility.SetDirty(spawner);
                    EndUndoGroup(g);
                    SceneView.RepaintAll();
                }
            }

            for (int i = 0; i < n; i++)
            {
                PolygonVertex v = verts[i];
                Vector3 w = tr.TransformPoint(poly.center + v.position);
                float size = HandleUtility.GetHandleSize(w) * VertexSizeMul;
                Handles.color = i == 0 ? FirstVertexColor : vertex;

                EditorGUI.BeginChangeCheck();
                Vector3 nw = Handles.FreeMoveHandle(w, size, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    if (IsSnapActive()) nw = SnapVec(nw, SnapStep);
                    int g = BeginUndoGroup(spawner, "Move Polygon Vertex");
                    v.position = tr.InverseTransformPoint(nw) - poly.center;
                    EditorUtility.SetDirty(spawner);
                    EndUndoGroup(g);
                    SceneView.RepaintAll();
                }

                Handles.Label(w + Vector3.up * 0.5f, i.ToString());
            }
        }

        /// <summary>Ear-clipping заливка полигона. Корректно для невыпуклых форм, 0 аллокаций.</summary>
        private static void FillPolygonFan(ReadOnlySpan<Vector3> pts)
        {
            int n = pts.Length;
            if (n < 3 || n > MaxTriangulatedVerts) return;

            Span<int> indices = new Span<int>(_triIndexBuffer);
            int triCount = PolygonTriangulator.Triangulate(pts, indices);
            if (triCount <= 0) return;

            for (int i = 0; i < triCount; i++)
            {
                _triRenderBuffer[0] = pts[indices[i * 3]];
                _triRenderBuffer[1] = pts[indices[i * 3 + 1]];
                _triRenderBuffer[2] = pts[indices[i * 3 + 2]];
                Handles.DrawAAConvexPolygon(_triRenderBuffer);
            }
        }
    }
}
#endif