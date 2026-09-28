using System;
using System.Collections.Generic;

namespace Glowbound.Core.Generation
{
    public static class BlockerGenerator
    {
        public static bool TryGenerate(int width, int height, int districtCount, int[] districtMap,
            int[] lanternCells, int targetCount, GenerationRandom random, out bool[] blocked)
        {
            blocked = new bool[width * height];
            if (targetCount == 0) return true;
            var lanternSet = new HashSet<int>(lanternCells);
            var candidates = new List<int>();
            for (var i = 0; i < blocked.Length; i++)
                if (!lanternSet.Contains(i)) candidates.Add(i);
            random.Shuffle(candidates);

            var placed = 0;
            for (var c = 0; c < candidates.Count && placed < targetCount; c++)
            {
                var cell = candidates[c];
                var district = districtMap[cell];
                blocked[cell] = true;
                if (!DistrictRemainsConnected(width, height, districtMap, blocked, district))
                {
                    blocked[cell] = false;
                    continue;
                }
                placed++;
            }
            return placed == targetCount;
        }

        private static bool DistrictRemainsConnected(int width, int height, int[] districtMap,
            bool[] blocked, int district)
        {
            var start = -1;
            var expected = 0;
            for (var i = 0; i < districtMap.Length; i++)
            {
                if (districtMap[i] != district || blocked[i]) continue;
                expected++;
                if (start < 0) start = i;
            }
            if (expected == 0) return false;

            var visited = new bool[districtMap.Length];
            var queue = new Queue<int>();
            queue.Enqueue(start);
            visited[start] = true;
            var reached = 0;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                reached++;
                var x = current % width;
                var y = current / width;
                TryVisit(current - 1, x > 0);
                TryVisit(current + 1, x + 1 < width);
                TryVisit(current - width, y > 0);
                TryVisit(current + width, y + 1 < height);
            }
            return reached == expected;

            void TryVisit(int candidate, bool inBounds)
            {
                if (!inBounds || visited[candidate] || blocked[candidate] || districtMap[candidate] != district) return;
                visited[candidate] = true;
                queue.Enqueue(candidate);
            }
        }
    }
}
