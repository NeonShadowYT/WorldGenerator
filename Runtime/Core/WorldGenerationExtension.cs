using UnityEngine;

namespace NeonImperium.WorldGenerations
{
    public abstract class WorldGenerationExtension : MonoBehaviour
    {
        public abstract void OnGameObjectSpawned(GameObject obj);
        public abstract void OnGenerationComplete();
    }
}