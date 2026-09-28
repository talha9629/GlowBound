using System;
using System.Collections.Generic;

namespace Glowbound.Core.Generation
{
    public static class SolutionGenerator
    {
        public static bool TryGenerate(int width, int height, int districtCount, int[] districtMap,
            GenerationRandom random, int maxNodes, out int[] lanternCells)
        {
            if (districtMap == null) throw new ArgumentNullException(nameof(districtMap));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (districtMap.Length != width * height) throw new ArgumentException("District map size mismatch.");

            var districtCells = BuildDistrictCells(districtCount, districtMap);
            var assigned = new int[districtCount];
            for (var i = 0; i < assigned.Length; i++) assigned[i] = -1;
            var rowUsed = new bool[height];
            var colUsed = new bool[width];
            var nodes = 0;

            if (!Search())
            {
                lanternCells = Array.Empty<int>();
                return false;
            }

            lanternCells = (int[])assigned.Clone();
            return true;

            bool Search()
            {
                if (++nodes > maxNodes) return false;
                var nextDistrict = SelectNextDistrict(out var candidates);
                if (nextDistrict < 0) return true;
                if (candidates.Count == 0) return false;
                random.Shuffle(candidates);
                for (var i = 0; i < candidates.Count; i++)
                {
                    var cell = candidates[i];
                    var row = cell / width;
                    var col = cell % width;
                    assigned[nextDistrict] = cell;
                    rowUsed[row] = true;
                    colUsed[col] = true;
                    if (Search()) return true;
                    assigned[nextDistrict] = -1;
                    rowUsed[row] = false;
                    colUsed[col] = false;
                }
                return false;
            }

            int SelectNextDistrict(out List<int> best)
            {
                var bestDistrict = -1;
                best = null;
                for (var d = 0; d < districtCount; d++)
                {
                    if (assigned[d] >= 0) continue;
                    var candidates = new List<int>();
                    var cells = districtCells[d];
                    for (var i = 0; i < cells.Count; i++)
                    {
                        var cell = cells[i];
                        var row = cell / width;
                        var col = cell % width;
                        if (!rowUsed[row] && !colUsed[col]) candidates.Add(cell);
                    }
                    if (best == null || candidates.Count < best.Count)
                    {
                        bestDistrict = d;
                        best = candidates;
                        if (best.Count == 0) break;
                    }
                }
                return bestDistrict;
            }
        }

        private static List<int>[] BuildDistrictCells(int districtCount, int[] districtMap)
        {
            var result = new List<int>[districtCount];
            for (var d = 0; d < districtCount; d++) result[d] = new List<int>();
            for (var i = 0; i < districtMap.Length; i++)
            {
                var d = districtMap[i];
                if (d < 0 || d >= districtCount) throw new ArgumentException("District id out of range.");
                result[d].Add(i);
            }
            return result;
        }
    }
}
