using System;
using System.Collections.Generic;

namespace Glowbound.Core
{
    public enum SolutionMultiplicity
    {
        NoSolution = 0,
        Unique = 1,
        Multiple = 2
    }

    public sealed class PuzzleSolveResult
    {
        public SolutionMultiplicity Multiplicity { get; }
        public int SolutionCount { get; }
        public int[] FirstSolutionLanternCells { get; }
        public long NodesVisited { get; }
        public long Backtracks { get; }
        public int MaxDepth { get; }

        public bool HasSolution
        {
            get { return SolutionCount > 0; }
        }

        internal PuzzleSolveResult(
            SolutionMultiplicity multiplicity,
            int solutionCount,
            int[] firstSolutionLanternCells,
            long nodesVisited,
            long backtracks,
            int maxDepth)
        {
            Multiplicity = multiplicity;
            SolutionCount = solutionCount;
            FirstSolutionLanternCells = firstSolutionLanternCells ?? Array.Empty<int>();
            NodesVisited = nodesVisited;
            Backtracks = backtracks;
            MaxDepth = maxDepth;
        }
    }

    public static class PuzzleSolver
    {
        public static PuzzleSolveResult Analyze(
            CompiledPuzzle puzzle,
            int maxSolutions = 2)
        {
            if (puzzle == null)
            {
                throw new ArgumentNullException(nameof(puzzle));
            }

            if (maxSolutions < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxSolutions),
                    "Uniqueness analysis requires a cutoff of at least two solutions.");
            }

            var search = new SearchState(puzzle, maxSolutions);
            search.Search(0);

            var multiplicity = search.SolutionCount == 0
                ? SolutionMultiplicity.NoSolution
                : search.SolutionCount == 1
                    ? SolutionMultiplicity.Unique
                    : SolutionMultiplicity.Multiple;

            return new PuzzleSolveResult(
                multiplicity,
                search.SolutionCount,
                search.FirstSolution,
                search.NodesVisited,
                search.Backtracks,
                search.MaxDepth);
        }

        private sealed class SearchState
        {
            private readonly CompiledPuzzle _puzzle;
            private readonly int _maxSolutions;
            private readonly int[] _assignment;
            private readonly bool[] _horizontalOccupied;
            private readonly bool[] _verticalOccupied;

            public int SolutionCount { get; private set; }
            public int[] FirstSolution { get; private set; } = Array.Empty<int>();
            public long NodesVisited { get; private set; }
            public long Backtracks { get; private set; }
            public int MaxDepth { get; private set; }

            public SearchState(CompiledPuzzle puzzle, int maxSolutions)
            {
                _puzzle = puzzle;
                _maxSolutions = maxSolutions;
                _assignment = new int[puzzle.DistrictCount];
                _horizontalOccupied = new bool[puzzle.HorizontalSegments.Length];
                _verticalOccupied = new bool[puzzle.VerticalSegments.Length];

                for (var i = 0; i < _assignment.Length; i++)
                {
                    _assignment[i] = -1;
                }
            }

            public void Search(int depth)
            {
                if (SolutionCount >= _maxSolutions)
                {
                    return;
                }

                NodesVisited++;
                if (depth > MaxDepth)
                {
                    MaxDepth = depth;
                }

                if (depth == _puzzle.DistrictCount)
                {
                    if (AreAllHousesSatisfied())
                    {
                        RecordSolution();
                    }

                    return;
                }

                var districtIndex = SelectNextDistrict(out var candidates);
                if (districtIndex < 0 || candidates.Count == 0)
                {
                    Backtracks++;
                    return;
                }

                for (var candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    if (SolutionCount >= _maxSolutions)
                    {
                        return;
                    }

                    var cellIndex = candidates[candidateIndex];
                    var before = SolutionCount;

                    Place(districtIndex, cellIndex);

                    if (HousesRemainReachable())
                    {
                        Search(depth + 1);
                    }

                    Remove(districtIndex, cellIndex);

                    if (SolutionCount == before)
                    {
                        Backtracks++;
                    }
                }
            }

            private int SelectNextDistrict(out List<int> bestCandidates)
            {
                var bestDistrict = -1;
                bestCandidates = null;

                for (var districtIndex = 0; districtIndex < _assignment.Length; districtIndex++)
                {
                    if (_assignment[districtIndex] >= 0)
                    {
                        continue;
                    }

                    var candidates = GetFeasibleCandidates(districtIndex);
                    if (candidates.Count == 0)
                    {
                        bestCandidates = candidates;
                        return districtIndex;
                    }

                    if (bestCandidates == null || candidates.Count < bestCandidates.Count)
                    {
                        bestDistrict = districtIndex;
                        bestCandidates = candidates;

                        if (bestCandidates.Count == 1)
                        {
                            break;
                        }
                    }
                }

                return bestDistrict;
            }

            private List<int> GetFeasibleCandidates(int districtIndex)
            {
                var result = new List<int>();
                var cells = _puzzle.DistrictCells[districtIndex];

                for (var i = 0; i < cells.Length; i++)
                {
                    var cellIndex = cells[i];
                    if (IsCandidateFeasible(cellIndex))
                    {
                        result.Add(cellIndex);
                    }
                }

                return result;
            }

            private bool IsCandidateFeasible(int cellIndex)
            {
                var horizontalSegment = _puzzle.CellToHorizontalSegment[cellIndex];
                var verticalSegment = _puzzle.CellToVerticalSegment[cellIndex];

                if (_horizontalOccupied[horizontalSegment] ||
                    _verticalOccupied[verticalSegment])
                {
                    return false;
                }

                _horizontalOccupied[horizontalSegment] = true;
                _verticalOccupied[verticalSegment] = true;

                var valid = !AnyHouseOverlit();

                _horizontalOccupied[horizontalSegment] = false;
                _verticalOccupied[verticalSegment] = false;

                return valid;
            }

            private void Place(int districtIndex, int cellIndex)
            {
                _assignment[districtIndex] = cellIndex;
                _horizontalOccupied[_puzzle.CellToHorizontalSegment[cellIndex]] = true;
                _verticalOccupied[_puzzle.CellToVerticalSegment[cellIndex]] = true;
            }

            private void Remove(int districtIndex, int cellIndex)
            {
                _assignment[districtIndex] = -1;
                _horizontalOccupied[_puzzle.CellToHorizontalSegment[cellIndex]] = false;
                _verticalOccupied[_puzzle.CellToVerticalSegment[cellIndex]] = false;
            }

            private bool AnyHouseOverlit()
            {
                for (var i = 0; i < _puzzle.Houses.Length; i++)
                {
                    var house = _puzzle.Houses[i];
                    if (CountIncoming(house) > house.Target)
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool HousesRemainReachable()
            {
                for (var houseIndex = 0; houseIndex < _puzzle.Houses.Length; houseIndex++)
                {
                    var house = _puzzle.Houses[houseIndex];
                    var current = CountIncoming(house);

                    if (current > house.Target)
                    {
                        return false;
                    }

                    var optimistic = current;

                    if (!IsDirectionOccupied(house, LightDirection.Up) &&
                        CanAnyUnassignedCandidateOccupy(house.UpSegmentId, false))
                    {
                        optimistic++;
                    }

                    if (!IsDirectionOccupied(house, LightDirection.Right) &&
                        CanAnyUnassignedCandidateOccupy(house.RightSegmentId, true))
                    {
                        optimistic++;
                    }

                    if (!IsDirectionOccupied(house, LightDirection.Down) &&
                        CanAnyUnassignedCandidateOccupy(house.DownSegmentId, false))
                    {
                        optimistic++;
                    }

                    if (!IsDirectionOccupied(house, LightDirection.Left) &&
                        CanAnyUnassignedCandidateOccupy(house.LeftSegmentId, true))
                    {
                        optimistic++;
                    }

                    if (optimistic < house.Target)
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool CanAnyUnassignedCandidateOccupy(
                int segmentId,
                bool horizontal)
            {
                if (segmentId < 0)
                {
                    return false;
                }

                for (var districtIndex = 0; districtIndex < _assignment.Length; districtIndex++)
                {
                    if (_assignment[districtIndex] >= 0)
                    {
                        continue;
                    }

                    var cells = _puzzle.DistrictCells[districtIndex];
                    for (var i = 0; i < cells.Length; i++)
                    {
                        var cellIndex = cells[i];

                        if (horizontal)
                        {
                            if (_puzzle.CellToHorizontalSegment[cellIndex] != segmentId)
                            {
                                continue;
                            }
                        }
                        else if (_puzzle.CellToVerticalSegment[cellIndex] != segmentId)
                        {
                            continue;
                        }

                        var horizontalSegment = _puzzle.CellToHorizontalSegment[cellIndex];
                        var verticalSegment = _puzzle.CellToVerticalSegment[cellIndex];

                        if (!_horizontalOccupied[horizontalSegment] &&
                            !_verticalOccupied[verticalSegment])
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private bool AreAllHousesSatisfied()
            {
                for (var i = 0; i < _puzzle.Houses.Length; i++)
                {
                    var house = _puzzle.Houses[i];
                    if (CountIncoming(house) != house.Target)
                    {
                        return false;
                    }
                }

                return true;
            }

            private int CountIncoming(HouseConstraint house)
            {
                var count = 0;

                if (house.UpSegmentId >= 0 &&
                    _verticalOccupied[house.UpSegmentId])
                {
                    count++;
                }

                if (house.RightSegmentId >= 0 &&
                    _horizontalOccupied[house.RightSegmentId])
                {
                    count++;
                }

                if (house.DownSegmentId >= 0 &&
                    _verticalOccupied[house.DownSegmentId])
                {
                    count++;
                }

                if (house.LeftSegmentId >= 0 &&
                    _horizontalOccupied[house.LeftSegmentId])
                {
                    count++;
                }

                return count;
            }

            private bool IsDirectionOccupied(
                HouseConstraint house,
                LightDirection direction)
            {
                var segmentId = house.GetSegmentId(direction);
                if (segmentId < 0)
                {
                    return false;
                }

                return direction == LightDirection.Left ||
                       direction == LightDirection.Right
                    ? _horizontalOccupied[segmentId]
                    : _verticalOccupied[segmentId];
            }

            private void RecordSolution()
            {
                SolutionCount++;

                if (FirstSolution.Length == 0)
                {
                    FirstSolution = (int[])_assignment.Clone();
                }
            }
        }
    }
}
