using System;
using System.Collections.Generic;

namespace Glowbound.Core
{
    public static class PuzzleCompiler
    {
        public static CompiledPuzzle Compile(PuzzleDefinition definition)
        {
            var validation = PuzzleDefinitionValidator.Validate(definition);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    "Cannot compile invalid puzzle: " + validation.Issues[0].Message);
            }

            var districtIds = CollectSortedDistrictIds(definition);
            var districtIdToIndex = new Dictionary<int, int>(districtIds.Length);
            for (var i = 0; i < districtIds.Length; i++)
            {
                districtIdToIndex.Add(districtIds[i], i);
            }

            var districtLists = new List<int>[districtIds.Length];
            for (var i = 0; i < districtLists.Length; i++)
            {
                districtLists[i] = new List<int>();
            }

            var cellToDistrictIndex = CreateFilledArray(definition.CellCount, -1);
            for (var cellIndex = 0; cellIndex < definition.Cells.Length; cellIndex++)
            {
                var cell = definition.Cells[cellIndex];
                if (cell.Kind != CellKind.District)
                {
                    continue;
                }

                var districtIndex = districtIdToIndex[cell.DistrictId];
                cellToDistrictIndex[cellIndex] = districtIndex;
                districtLists[districtIndex].Add(cellIndex);
            }

            var districtCells = new int[districtLists.Length][];
            for (var i = 0; i < districtLists.Length; i++)
            {
                districtCells[i] = districtLists[i].ToArray();
            }

            var cellToHorizontalSegment = CreateFilledArray(definition.CellCount, -1);
            var horizontalSegments = BuildHorizontalSegments(definition, cellToHorizontalSegment);

            var cellToVerticalSegment = CreateFilledArray(definition.CellCount, -1);
            var verticalSegments = BuildVerticalSegments(definition, cellToVerticalSegment);

            var houses = BuildHouses(
                definition,
                cellToHorizontalSegment,
                cellToVerticalSegment);

            return new CompiledPuzzle(
                definition,
                districtIds,
                districtCells,
                cellToDistrictIndex,
                cellToHorizontalSegment,
                cellToVerticalSegment,
                horizontalSegments,
                verticalSegments,
                houses);
        }

        private static int[] CollectSortedDistrictIds(PuzzleDefinition definition)
        {
            var ids = new HashSet<int>();
            for (var i = 0; i < definition.Cells.Length; i++)
            {
                if (definition.Cells[i].Kind == CellKind.District)
                {
                    ids.Add(definition.Cells[i].DistrictId);
                }
            }

            var result = new int[ids.Count];
            ids.CopyTo(result);
            Array.Sort(result);
            return result;
        }

        private static VisibilitySegment[] BuildHorizontalSegments(
            PuzzleDefinition definition,
            int[] cellToSegment)
        {
            var segments = new List<VisibilitySegment>();

            for (var y = 0; y < definition.Height; y++)
            {
                var x = 0;
                while (x < definition.Width)
                {
                    var index = definition.ToIndex(x, y);
                    if (definition.Cells[index].Kind != CellKind.District)
                    {
                        x++;
                        continue;
                    }

                    var segmentId = segments.Count;
                    var cells = new List<int>();

                    while (x < definition.Width)
                    {
                        index = definition.ToIndex(x, y);
                        if (definition.Cells[index].Kind != CellKind.District)
                        {
                            break;
                        }

                        cellToSegment[index] = segmentId;
                        cells.Add(index);
                        x++;
                    }

                    segments.Add(new VisibilitySegment(segmentId, true, cells.ToArray()));
                }
            }

            return segments.ToArray();
        }

        private static VisibilitySegment[] BuildVerticalSegments(
            PuzzleDefinition definition,
            int[] cellToSegment)
        {
            var segments = new List<VisibilitySegment>();

            for (var x = 0; x < definition.Width; x++)
            {
                var y = 0;
                while (y < definition.Height)
                {
                    var index = definition.ToIndex(x, y);
                    if (definition.Cells[index].Kind != CellKind.District)
                    {
                        y++;
                        continue;
                    }

                    var segmentId = segments.Count;
                    var cells = new List<int>();

                    while (y < definition.Height)
                    {
                        index = definition.ToIndex(x, y);
                        if (definition.Cells[index].Kind != CellKind.District)
                        {
                            break;
                        }

                        cellToSegment[index] = segmentId;
                        cells.Add(index);
                        y++;
                    }

                    segments.Add(new VisibilitySegment(segmentId, false, cells.ToArray()));
                }
            }

            return segments.ToArray();
        }

        private static HouseConstraint[] BuildHouses(
            PuzzleDefinition definition,
            int[] horizontalSegments,
            int[] verticalSegments)
        {
            var houses = new List<HouseConstraint>();

            for (var index = 0; index < definition.Cells.Length; index++)
            {
                var cell = definition.Cells[index];
                if (cell.Kind != CellKind.House)
                {
                    continue;
                }

                definition.ToCoordinates(index, out var x, out var y);

                houses.Add(new HouseConstraint(
                    index,
                    cell.HouseTarget,
                    GetAdjacentSegment(definition, verticalSegments, x, y - 1),
                    GetAdjacentSegment(definition, horizontalSegments, x + 1, y),
                    GetAdjacentSegment(definition, verticalSegments, x, y + 1),
                    GetAdjacentSegment(definition, horizontalSegments, x - 1, y)));
            }

            return houses.ToArray();
        }

        private static int GetAdjacentSegment(
            PuzzleDefinition definition,
            int[] cellToSegment,
            int x,
            int y)
        {
            if (!definition.IsInBounds(x, y))
            {
                return -1;
            }

            var index = definition.ToIndex(x, y);
            return definition.Cells[index].Kind == CellKind.District
                ? cellToSegment[index]
                : -1;
        }

        private static int[] CreateFilledArray(int length, int value)
        {
            var result = new int[length];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = value;
            }

            return result;
        }
    }
}
