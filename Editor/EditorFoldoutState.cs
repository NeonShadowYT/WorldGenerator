#if UNITY_EDITOR
namespace NeonImperium.WorldGenerations
{
    public static class EditorFoldoutState
    {
        public static bool SpawnSettings = true;
        public static bool ClusteringSettings = false;
        public static bool RaySettings = false;
        public static bool StabilitySettings = false;
        public static bool AvoidanceSettings = false;
        public static bool RuntimeSettings = false;
        public static bool NavMeshSettings = false;
    }
}
#endif