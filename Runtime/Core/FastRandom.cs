namespace NeonImperium.WorldGenerations
{
    /// <summary>
    /// Xorshift128 PRNG. Детерминирован, без боксинга и виртуальных вызовов.
    /// ~5× быстрее System.Random в горячих путях генерации.
    /// </summary>
    public sealed class FastRandom
    {
        private uint _x, _y, _z, _w;

        public FastRandom(int seed)
        {
            uint s = (uint)seed;
            if (s == 0u) s = 0x9E3779B9u;
            _x = SplitMix(ref s);
            _y = SplitMix(ref s);
            _z = SplitMix(ref s);
            _w = SplitMix(ref s);
            if ((_x | _y | _z | _w) == 0u) _w = 1u; // защита от нулевого состояния
        }

        private static uint SplitMix(ref uint state)
        {
            state += 0x9E3779B9u;
            uint z = state;
            z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
            z = (z ^ (z >> 13)) * 0xC2B2AE35u;
            return z ^ (z >> 16);
        }

        public uint NextUInt()
        {
            uint t = _x ^ (_x << 11);
            _x = _y; _y = _z; _z = _w;
            _w = _w ^ (_w >> 19) ^ (t ^ (t >> 8));
            return _w;
        }

        /// <summary>[0..1) float, 24 бита мантиссы — достаточно для игровых величин.</summary>
        public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

        /// <summary>[0..1) double.</summary>
        public double NextDouble() => (NextUInt() >> 8) * (1.0 / 16777216.0);

        /// <summary>[0..maxValue) int. Lemire range reduction, bias < 1/maxValue.</summary>
        public int Next(int maxValue)
        {
            ulong prod = (ulong)NextUInt() * (uint)maxValue;
            return (int)(prod >> 32);
        }

        /// <summary>[min..max) int.</summary>
        public int Next(int minValue, int maxValue) => minValue + Next(maxValue - minValue);

        public float Range(float min, float max) => min + (max - min) * NextFloat();
        public float Range(float max) => max * NextFloat();
        public double Range(double min, double max) => min + (max - min) * NextDouble();
    }
}