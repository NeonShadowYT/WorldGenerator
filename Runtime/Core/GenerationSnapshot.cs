using System.Collections.Generic;
using UnityEngine;

namespace NeonImperium.WorldGenerations
{
    /// <summary>Минимальный слепок состояния генератора для save/load и воспроизводимости.</summary>
    [System.Serializable]
    public struct GenerationSnapshot
    {
        public int seed;
        public int zoneHash;
        public int population;
        public Vector3 dimensions;
    }

    /// <summary>FNV-1a хэш и JSON (de)сериализация для GenerationSnapshot. Без аллокаций в горячем пути.</summary>
    public static class GenerationSnapshotSerializer
    {
        private const uint FnvOffset = 2166136261;
        private const uint FnvPrime = 16777619;

        /// <summary>Хэш зон по типу и bounds. Используется для проверки совместимости snapshot с текущей сценой.</summary>
        public static int ComputeZoneHash(List<ISpawnZone> zones)
        {
            if (zones == null || zones.Count == 0) return 0;

            uint hash = FnvOffset;
            hash = HashInt(hash, zones.Count);

            for (int i = 0; i < zones.Count; i++)
            {
                ISpawnZone z = zones[i];
                if (z == null) continue;

                hash = HashInt(hash, z.GetType().GetHashCode());

                Bounds b = z.LocalBounds;
                hash = HashFloat(hash, b.center.x);
                hash = HashFloat(hash, b.center.y);
                hash = HashFloat(hash, b.center.z);
                hash = HashFloat(hash, b.size.x);
                hash = HashFloat(hash, b.size.y);
                hash = HashFloat(hash, b.size.z);
            }

            return (int)hash;
        }

        public static string Serialize(GenerationSnapshot snapshot) => JsonUtility.ToJson(snapshot);

        public static GenerationSnapshot Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return default;
            return JsonUtility.FromJson<GenerationSnapshot>(json);
        }

        private static uint HashInt(uint hash, int value)
        {
            hash = FnvByte(hash, (byte)(value & 0xFF));
            hash = FnvByte(hash, (byte)((value >> 8) & 0xFF));
            hash = FnvByte(hash, (byte)((value >> 16) & 0xFF));
            hash = FnvByte(hash, (byte)((value >> 24) & 0xFF));
            return hash;
        }

        private static uint HashFloat(uint hash, float value)
        {
            return HashInt(hash, System.BitConverter.SingleToInt32Bits(value));
        }

        private static uint FnvByte(uint hash, byte b)
        {
            hash ^= b;
            hash *= FnvPrime;
            return hash;
        }
    }
}