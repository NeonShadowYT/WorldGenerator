#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NeonImperium.WorldGenerations
{
    /// <summary>Демо-зоны, создаваемые при первом переключении в режим Polygon.</summary>
    public static class SpawnZoneDefaults
    {
        // Шестиугольник, вертикальный размер 4 м (по 2 м вверх/вниз от центра)
        private static readonly Vector3[] HexagonPos =
        {
            new( 0f, 0f,  8f),
            new( 7f, 0f,  4f),
            new( 7f, 0f, -4f),
            new( 0f, 0f, -8f),
            new(-7f, 0f, -4f),
            new(-7f, 0f,  4f),
        };

        // Треугольник сбоку — с разными высотами граней, демонстрирует 3D
        private static readonly Vector3[] TrianglePos =
        {
            new( 0f, 2f,  4f),
            new( 4f, 0f, -3f),
            new(-4f, 0f, -3f),
        };

        private static readonly float[] TriangleSize = { 6f, 3f, 3f };

        private const float HexagonSize = 4f;
        private const float TriangleOffsetX = 22f;

        public static void FillPolygonDefaults(SerializedProperty polygonsProp)
        {
            if (polygonsProp == null || polygonsProp.arraySize > 0) return;

            polygonsProp.arraySize = 2;

            SerializedProperty hexZone = polygonsProp.GetArrayElementAtIndex(0);
            hexZone.FindPropertyRelative("enabled").boolValue = true;
            hexZone.FindPropertyRelative("center").vector3Value = Vector3.zero;
            WriteVertices(hexZone.FindPropertyRelative("vertices"), HexagonPos, HexagonSize);

            SerializedProperty triZone = polygonsProp.GetArrayElementAtIndex(1);
            triZone.FindPropertyRelative("enabled").boolValue = true;
            triZone.FindPropertyRelative("center").vector3Value = new Vector3(TriangleOffsetX, 0f, 0f);
            WriteVerticesVariable(triZone.FindPropertyRelative("vertices"), TrianglePos, TriangleSize);
        }

        private static void WriteVertices(SerializedProperty verticesProp, Vector3[] positions, float size)
        {
            verticesProp.arraySize = positions.Length;
            for (int i = 0; i < positions.Length; i++)
            {
                SerializedProperty v = verticesProp.GetArrayElementAtIndex(i);
                v.FindPropertyRelative("position").vector3Value = positions[i];
                v.FindPropertyRelative("verticalSize").floatValue = size;
            }
        }

        private static void WriteVerticesVariable(SerializedProperty verticesProp, Vector3[] positions, float[] sizes)
        {
            verticesProp.arraySize = positions.Length;
            for (int i = 0; i < positions.Length; i++)
            {
                SerializedProperty v = verticesProp.GetArrayElementAtIndex(i);
                v.FindPropertyRelative("position").vector3Value = positions[i];
                v.FindPropertyRelative("verticalSize").floatValue = sizes[i];
            }
        }
    }
}
#endif