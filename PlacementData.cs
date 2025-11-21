namespace NeonImperium
{
    public enum WorldSpawnType { Object, Enemy }
    public enum RayOriginType { TopFace, SideFaces, InsideVolume }
    public enum RayCastType { Ray, Sphere }
    
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
        ClusterFailed
    }
}