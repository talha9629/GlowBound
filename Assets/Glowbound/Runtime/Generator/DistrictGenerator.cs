using System;
using System.Collections.Generic;

namespace Glowbound.Core.Generation
{
    public static class DistrictGenerator
    {
        public static int[] Generate(int width, int height, int districtCount, GenerationRandom random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            var cellCount = width * height;
            var owner = new int[cellCount];
            var sizes = new int[districtCount];
            for (var i = 0; i < owner.Length; i++) owner[i] = -1;

            var seeds = new List<int>(cellCount);
            for (var i = 0; i < cellCount; i++) seeds.Add(i);
            random.Shuffle(seeds);
            for (var d = 0; d < districtCount; d++)
            {
                owner[seeds[d]] = d;
                sizes[d] = 1;
            }

            var remaining = cellCount - districtCount;
            var frontier = new List<int>();
            while (remaining > 0)
            {
                var minSize = int.MaxValue;
                for (var d = 0; d < districtCount; d++)
                    if (HasFrontier(owner, width, height, d) && sizes[d] < minSize) minSize = sizes[d];
                var candidateDistricts = new List<int>();
                for (var d = 0; d < districtCount; d++)
                    if (sizes[d] == minSize && HasFrontier(owner, width, height, d)) candidateDistricts.Add(d);

                if (candidateDistricts.Count == 0)
                    throw new InvalidOperationException("District growth stalled before all cells were assigned.");

                var chosenDistrict = candidateDistricts[random.NextInt(candidateDistricts.Count)];
                CollectFrontier(owner, width, height, chosenDistrict, frontier);
                var chosenCell = frontier[random.NextInt(frontier.Count)];
                owner[chosenCell] = chosenDistrict;
                sizes[chosenDistrict]++;
                remaining--;
            }

            return owner;
        }

        private static bool HasFrontier(int[] owner, int width, int height, int district)
        {
            for (var i = 0; i < owner.Length; i++)
            {
                if (owner[i] != district) continue;
                if (HasUnassignedNeighbor(owner, width, height, i)) return true;
            }
            return false;
        }

        private static void CollectFrontier(int[] owner, int width, int height, int district, List<int> result)
        {
            result.Clear();
            var seen = new HashSet<int>();
            for (var i = 0; i < owner.Length; i++)
            {
                if (owner[i] != district) continue;
                AddUnassignedNeighbors(owner, width, height, i, result, seen);
            }
        }
        private static bool HasUnassignedNeighbor(int[] owner, int width, int height, int index)
        {
            var x = index % width;
            var y = index / width;
            return (x > 0 && owner[index - 1] < 0) ||
                   (x + 1 < width && owner[index + 1] < 0) ||
                   (y > 0 && owner[index - width] < 0) ||
                   (y + 1 < height && owner[index + width] < 0);
        }

        private static void AddUnassignedNeighbors(int[] owner, int width, int height, int index,
            List<int> result, HashSet<int> seen)
        {
            var x = index % width;
            var y = index / width;
            TryAdd(index - 1, x > 0);
            TryAdd(index + 1, x + 1 < width);
            TryAdd(index - width, y > 0);
            TryAdd(index + width, y + 1 < height);

            void TryAdd(int candidate, bool inBounds)
            {
                if (!inBounds || owner[candidate] >= 0 || !seen.Add(candidate)) return;
                result.Add(candidate);
            }
        }
    }
}
