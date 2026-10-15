using UnityEngine;

namespace NeonImperium.WorldGenerations
{
    public enum GenerationAlgorithmType { Standard }
    public enum RayOriginType { TopFace, SideFaces, InsideVolume }
    public enum RayCastType { Ray, Sphere }

    /// <summary>Режим проверки потолка над точкой размещения.</summary>
    public enum CeilingCheckMode
    {
        None = 0,           // проверка отключена
        RequireOpenSky = 1, // над точкой не должно быть препятствий
        RequireRoof = 2     // над точкой обязательно должна быть крыша
    }

    /// <summary>Режим авто-перегенерации NavMesh Surface (используется только в редакторе).</summary>
    public enum NavMeshRegenerationMode
    {
        Never = 0,
        OnSingleGeneration = 1,
        OnFullRegeneration = 2,
        Always = 3
    }

    public enum FailureReasonType
    {
        NoHit,
        CeilingCheck,
        EdgeCheck,
        FloorCheck,
        NearObstacle,
        InvalidLayer,
        OutOfBounds,
        TooCloseToOther,
        ClusterFailed,
        NoNavMesh,
        InvalidSurfaceAngle
    }

    /// <summary>Точка-кандидат. zoneIndex = -1 означает «невалидная точка» (нет зон/кластеров).</summary>
    public struct PlacementPoint
    {
        public Vector3 localPosition;
        public int zoneIndex;
    }

    public struct PlacementData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public GameObject prefab;
        public bool isValid;
        public FailureReasonType failureReason;
    }

    public struct DebugRay
    {
        public Vector3 start;
        public Vector3 end;
        public Color color;
        public DebugRayType rayType;
    }

    public enum DebugRayType
    {
        Main = 0,
        Stability = 1,
        Floor = 2,
        Avoidance = 3,
        Ceiling = 4
    }
}