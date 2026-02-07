using UnityEngine;

namespace NeonImperium.WorldGeneration
{
    public struct PlacementPoint { public Vector3 localPosition; }

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