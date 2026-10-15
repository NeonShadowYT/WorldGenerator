using System.Collections.Generic;
using UnityEngine;

namespace NeonImperium.WorldGenerations
{
    public enum SpawnZoneMode { Box = 0, Polygon = 1 }

    [System.Serializable]
    public class BoxSpawnZoneSettings
    {
        public bool enabled = true;
        public Vector3 center = Vector3.zero;
        public Vector3 size = new(30f, 10f, 30f);
    }

    /// <summary>
    /// Вершина полигональной зоны — центр вертикальной грани.
    /// position — точка ребра в 3D, VerticalSize — размер грани вверх и вниз от центра.
    /// </summary>
    [System.Serializable]
    public class PolygonVertex
    {
        public const float MinVerticalSize = 0.1f;
        public const float MaxVerticalSize = 10000f;

        public Vector3 position = Vector3.zero;

        [SerializeField] private float verticalSize = 4f;

        public float VerticalSize
        {
            get => verticalSize;
            set => verticalSize = Mathf.Clamp(value, MinVerticalSize, MaxVerticalSize);
        }

        public PolygonVertex() { }

        public PolygonVertex(Vector3 pos, float size)
        {
            position = pos;
            VerticalSize = size;
        }
    }

    [System.Serializable]
    public class PolygonSpawnZoneSettings
    {
        public bool enabled = true;

        /// <summary>Смещение всей зоны в локальных координатах спавнера. Тянется за оранжевый handle в Scene View.</summary>
        public Vector3 center = Vector3.zero;

        /// <summary>Вершины относительно center. Каждая — центр вертикальной грани.</summary>
        public List<PolygonVertex> vertices = new()
        {
            new PolygonVertex(new Vector3( 0f, 0f,  8f), 4f),
            new PolygonVertex(new Vector3( 7f, 0f,  4f), 4f),
            new PolygonVertex(new Vector3( 7f, 0f, -4f), 4f),
            new PolygonVertex(new Vector3( 0f, 0f, -8f), 4f),
            new PolygonVertex(new Vector3(-7f, 0f, -4f), 4f),
            new PolygonVertex(new Vector3(-7f, 0f,  4f), 4f),
        };
    }
}