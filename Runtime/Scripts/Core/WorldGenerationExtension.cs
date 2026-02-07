using UnityEngine;

namespace NeonImperium.WorldGeneration
{
    public abstract class WorldGenerationExtension : MonoBehaviour
    {
        public abstract void OnGameObjectSpawned(GameObject obj);
        public abstract void OnGenerationComplete();
    }
}