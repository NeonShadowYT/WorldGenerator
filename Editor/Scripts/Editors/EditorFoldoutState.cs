#if UNITY_EDITOR

namespace NeonImperium.WorldGeneration
{
    public static class EditorFoldoutState
    {
        #pragma warning disable UDR0001
        public static bool SpawnSettings = true;
        public static bool ClusteringSettings = false;
        public static bool RaySettings = false;
        public static bool StabilitySettings = false;
        public static bool AvoidanceSettings = false;
        #pragma warning restore UDR0001
    }
}
#endif