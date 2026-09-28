using System;
using System.Collections.Generic;

namespace Glowbound.Core.Generation
{
    /// <summary>Small deterministic PRNG whose sequence is independent of Unity/.NET Random.</summary>
    public sealed class GenerationRandom
    {
        private ulong _state;

        public GenerationRandom(int seed)
        {
            _state = MixSeed(unchecked((uint)seed));
            if (_state == 0) _state = 0x9E3779B97F4A7C15UL;
        }

        public uint NextUInt()
        {
            // xorshift64*; fixed algorithm = stable procedural generation across runtimes.
            var x = _state;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            _state = x;
            return (uint)((x * 2685821657736338717UL) >> 32);
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return (int)(NextUInt() % (uint)maxExclusive);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return minInclusive + NextInt(maxExclusive - minInclusive);
        }

        public bool Chance(float probability)
        {
            if (probability <= 0f) return false;
            if (probability >= 1f) return true;
            return NextUInt() / (float)uint.MaxValue < probability;
        }

        public void Shuffle<T>(IList<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = NextInt(i + 1);
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        private static ulong MixSeed(uint seed)
        {
            ulong z = seed + 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
