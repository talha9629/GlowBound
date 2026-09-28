using System;
using System.Collections.Generic;

namespace Glowbound.Core
{
    public enum PuzzleValidationCode
    {
        NullDefinition,
        InvalidDimensions,
        CellCountMismatch,
        InvalidCellKind,
        InvalidDistrictId,
        UnexpectedDistrictId,
        InvalidHouseTarget,
        UnexpectedHouseTarget,
        NoDistricts,
        DisconnectedDistrict,
        ImpossibleHouseTarget
    }

    public readonly struct PuzzleValidationIssue
    {
        public readonly PuzzleValidationCode Code;
        public readonly int CellIndex;
        public readonly int DistrictId;
        public readonly string Message;

        public PuzzleValidationIssue(
            PuzzleValidationCode code,
            string message,
            int cellIndex = -1,
            int districtId = CellDefinition.NoDistrict)
        {
            Code = code;
            CellIndex = cellIndex;
            DistrictId = districtId;
            Message = message ?? string.Empty;
        }

        public override string ToString()
        {
            return Message;
        }
    }

    public sealed class PuzzleValidationResult
    {
        private readonly List<PuzzleValidationIssue> _issues = new List<PuzzleValidationIssue>();

        public IReadOnlyList<PuzzleValidationIssue> Issues
        {
            get { return _issues; }
        }

        public bool IsValid
        {
            get { return _issues.Count == 0; }
        }

        internal void Add(PuzzleValidationIssue issue)
        {
            _issues.Add(issue);
        }

        public bool HasIssue(PuzzleValidationCode code)
        {
            for (var i = 0; i < _issues.Count; i++)
            {
                if (_issues[i].Code == code)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class PuzzleDefinitionValidator
    {
        public static PuzzleValidationResult Validate(PuzzleDefinition definition)
        {
            var result = new PuzzleValidationResult();

            if (definition == null)
            {
                result.Add(new PuzzleValidationIssue(
                    PuzzleValidationCode.NullDefinition,
                    "Puzzle definition is null."));
                return result;
            }

            if (definition.Width <= 0 || definition.Height <= 0)
            {
                result.Add(new PuzzleValidationIssue(
                    PuzzleValidationCode.InvalidDimensions,
                    "Puzzle width and height must both be greater than zero."));
                return result;
            }

            var expectedCellCount = definition.Width * definition.Height;
            if (definition.Cells == null || definition.Cells.Length != expectedCellCount)
            {
                result.Add(new PuzzleValidationIssue(
                    PuzzleValidationCode.CellCountMismatch,
                    "Cell array length must equal width * height."));
                return result;
            }

            var districtCells = new Dictionary<int, List<int>>();

            for (var index = 0; index < definition.Cells.Length; index++)
            {
                var cell = definition.Cells[index];
                if (!Enum.IsDefined(typeof(CellKind), cell.Kind))
                {
                    result.Add(new PuzzleValidationIssue(
                        PuzzleValidationCode.InvalidCellKind,
                        "Cell has an unknown cell kind.",
                        index));
                    continue;
                }

                switch (cell.Kind)
                {
                    case CellKind.District:
                        if (cell.DistrictId < 0)
                        {
                            result.Add(new PuzzleValidationIssue(
                                PuzzleValidationCode.InvalidDistrictId,
                                "District cells must have a non-negative district id.",
                                index,
                                cell.DistrictId));
                        }
                        else
                        {
                            if (!districtCells.TryGetValue(cell.DistrictId, out var list))
                            {
                                list = new List<int>();
                                districtCells.Add(cell.DistrictId, list);
                            }

                            list.Add(index);
                        }

                        if (cell.HouseTarget != 0)
                        {
                            result.Add(new PuzzleValidationIssue(
                                PuzzleValidationCode.UnexpectedHouseTarget,
                                "District cells cannot have a house target.",
                                index,
                                cell.DistrictId));
                        }
                        break;

                    case CellKind.House:
                        if (cell.DistrictId != CellDefinition.NoDistrict)
                        {
                            result.Add(new PuzzleValidationIssue(
                                PuzzleValidationCode.UnexpectedDistrictId,
                                "House cells cannot belong to a district.",
                                index,
                                cell.DistrictId));
                        }

                        if (cell.HouseTarget < 1 || cell.HouseTarget > 3)
                        {
                            result.Add(new PuzzleValidationIssue(
                                PuzzleValidationCode.InvalidHouseTarget,
                                "House target must be 1, 2, or 3.",
                                index));
                        }
                        break;

                    case CellKind.Wall:
                        if (cell.DistrictId != CellDefinition.NoDistrict)
                        {
                            result.Add(new PuzzleValidationIssue(
                                PuzzleValidationCode.UnexpectedDistrictId,
                                "Wall cells cannot belong to a district.",
                                index,
                                cell.DistrictId));
                        }

                        if (cell.HouseTarget != 0)
                        {
                            result.Add(new PuzzleValidationIssue(
                                PuzzleValidationCode.UnexpectedHouseTarget,
                                "Wall cells cannot have a house target.",
                                index));
                        }
                        break;
                }
            }

            if (districtCells.Count == 0)
            {
                result.Add(new PuzzleValidationIssue(
                    PuzzleValidationCode.NoDistricts,
                    "Puzzle must contain at least one playable district."));
            }

            foreach (var pair in districtCells)
            {
                if (!IsDistrictConnected(definition, pair.Value))
                {
                    result.Add(new PuzzleValidationIssue(
                        PuzzleValidationCode.DisconnectedDistrict,
                        "District " + pair.Key + " is not orthogonally connected.",
                        districtId: pair.Key));
                }
            }

            for (var index = 0; index < definition.Cells.Length; index++)
            {
                var cell = definition.Cells[index];
                if (cell.Kind != CellKind.House || cell.HouseTarget < 1 || cell.HouseTarget > 3)
                {
                    continue;
                }

                var possibleDirections = CountAdjacentPlayableDirections(definition, index);
                if (possibleDirections < cell.HouseTarget)
                {
                    result.Add(new PuzzleValidationIssue(
                        PuzzleValidationCode.ImpossibleHouseTarget,
                        "House target exceeds its number of geometrically possible incoming directions.",
                        index));
                }
            }

            return result;
        }

        private static bool IsDistrictConnected(PuzzleDefinition definition, List<int> cells)
        {
            if (cells.Count <= 1)
            {
                return true;
            }

            var allowed = new HashSet<int>(cells);
            var visited = new HashSet<int>();
            var queue = new Queue<int>();

            queue.Enqueue(cells[0]);
            visited.Add(cells[0]);

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                definition.ToCoordinates(index, out var x, out var y);

                VisitNeighbor(definition, x, y - 1, allowed, visited, queue);
                VisitNeighbor(definition, x + 1, y, allowed, visited, queue);
                VisitNeighbor(definition, x, y + 1, allowed, visited, queue);
                VisitNeighbor(definition, x - 1, y, allowed, visited, queue);
            }

            return visited.Count == cells.Count;
        }

        private static void VisitNeighbor(
            PuzzleDefinition definition,
            int x,
            int y,
            HashSet<int> allowed,
            HashSet<int> visited,
            Queue<int> queue)
        {
            if (!definition.IsInBounds(x, y))
            {
                return;
            }

            var index = definition.ToIndex(x, y);
            if (allowed.Contains(index) && visited.Add(index))
            {
                queue.Enqueue(index);
            }
        }

        private static int CountAdjacentPlayableDirections(PuzzleDefinition definition, int houseIndex)
        {
            definition.ToCoordinates(houseIndex, out var x, out var y);
            var count = 0;

            if (IsDistrictCell(definition, x, y - 1)) count++;
            if (IsDistrictCell(definition, x + 1, y)) count++;
            if (IsDistrictCell(definition, x, y + 1)) count++;
            if (IsDistrictCell(definition, x - 1, y)) count++;

            return count;
        }

        private static bool IsDistrictCell(PuzzleDefinition definition, int x, int y)
        {
            return definition.IsInBounds(x, y) &&
                   definition.GetCell(x, y).Kind == CellKind.District;
        }
    }
}
