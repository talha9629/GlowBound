using System;
using System.Collections.Generic;

namespace Glowbound.Core.Generation
{
    public static class ClueDeriver
    {
        public static bool TryBuildDefinition(string id, int width, int height, int districtCount,
            int[] districtMap, int[] lanternCells, bool[] blocked, GenerationSettings settings,
            GenerationRandom random, out PuzzleDefinition definition, out int houseCount)
        {
            var cells = new CellDefinition[width * height];
            for (var i = 0; i < cells.Length; i++)
                cells[i] = blocked[i] ? CellDefinition.Wall() : CellDefinition.District(districtMap[i]);

            var lanternSet = new HashSet<int>(lanternCells);
            var eligible = new List<HouseCandidate>();
            for (var i = 0; i < blocked.Length; i++)
            {
                if (!blocked[i]) continue;
                var target = CountIncomingDirections(width, height, i, lanternSet, blocked);
                if (target >= 1 && target <= 3) eligible.Add(new HouseCandidate(i, (byte)target));
            }

            if (eligible.Count < settings.MinimumHouseCount)
            {
                definition = null;
                houseCount = 0;
                return false;
            }

            random.Shuffle(eligible);
            var desired = (int)Math.Round(settings.BlockerCount * settings.HouseFraction);
            desired = Math.Max(settings.MinimumHouseCount, desired);
            desired = Math.Min(desired, eligible.Count);
            for (var i = 0; i < desired; i++)
                cells[eligible[i].CellIndex] = CellDefinition.House(eligible[i].Target);

            houseCount = desired;
            definition = new PuzzleDefinition(id, width, height, cells);
            return true;
        }

        private static int CountIncomingDirections(int width, int height, int origin,
            HashSet<int> lanterns, bool[] blocked)
        {
            var x = origin % width;
            var y = origin / width;
            var count = 0;
            if (SeesLantern(x, y, 0, -1)) count++;
            if (SeesLantern(x, y, 1, 0)) count++;
            if (SeesLantern(x, y, 0, 1)) count++;
            if (SeesLantern(x, y, -1, 0)) count++;
            return count;

            bool SeesLantern(int sx, int sy, int dx, int dy)
            {
                var cx = sx + dx;
                var cy = sy + dy;
                while (cx >= 0 && cx < width && cy >= 0 && cy < height)
                {
                    var index = cy * width + cx;
                    if (lanterns.Contains(index)) return true;
                    if (blocked[index]) return false;
                    cx += dx;
                    cy += dy;
                }
                return false;
            }
        }

        private readonly struct HouseCandidate
        {
            public readonly int CellIndex;
            public readonly byte Target;
            public HouseCandidate(int cellIndex, byte target)
            {
                CellIndex = cellIndex;
                Target = target;
            }
        }
    }
}
