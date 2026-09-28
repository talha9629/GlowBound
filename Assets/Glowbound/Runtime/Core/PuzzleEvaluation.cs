using System;
using System.Collections.Generic;

namespace Glowbound.Core
{
    public enum RuleViolationCode
    {
        StateOnBlockedCell,
        DistrictOverfilled,
        LanternVisibility,
        HouseOverlit
    }

    public readonly struct RuleViolation
    {
        public readonly RuleViolationCode Code;
        public readonly int CellIndex;
        public readonly int RelatedIndex;
        public readonly string Message;

        public RuleViolation(
            RuleViolationCode code,
            string message,
            int cellIndex = -1,
            int relatedIndex = -1)
        {
            Code = code;
            CellIndex = cellIndex;
            RelatedIndex = relatedIndex;
            Message = message ?? string.Empty;
        }
    }

    public readonly struct HouseEvaluation
    {
        public readonly int CellIndex;
        public readonly byte Target;
        public readonly LightDirectionMask IncomingMask;
        public readonly int IncomingCount;

        public bool IsSatisfied
        {
            get { return IncomingCount == Target; }
        }

        public HouseEvaluation(
            int cellIndex,
            byte target,
            LightDirectionMask incomingMask,
            int incomingCount)
        {
            CellIndex = cellIndex;
            Target = target;
            IncomingMask = incomingMask;
            IncomingCount = incomingCount;
        }
    }

    public sealed class PuzzleEvaluationResult
    {
        public int[] DistrictLanternCounts { get; }
        public int[] HorizontalSegmentLanternCounts { get; }
        public int[] VerticalSegmentLanternCounts { get; }
        public HouseEvaluation[] Houses { get; }
        public IReadOnlyList<RuleViolation> Violations { get; }
        public bool HasContradiction { get; }
        public bool IsSolved { get; }

        internal PuzzleEvaluationResult(
            int[] districtLanternCounts,
            int[] horizontalSegmentLanternCounts,
            int[] verticalSegmentLanternCounts,
            HouseEvaluation[] houses,
            List<RuleViolation> violations,
            bool isSolved)
        {
            DistrictLanternCounts = districtLanternCounts;
            HorizontalSegmentLanternCounts = horizontalSegmentLanternCounts;
            VerticalSegmentLanternCounts = verticalSegmentLanternCounts;
            Houses = houses;
            Violations = violations.AsReadOnly();
            HasContradiction = violations.Count > 0;
            IsSolved = isSolved;
        }
    }

    public static class PuzzleEvaluator
    {
        public static PuzzleEvaluationResult Evaluate(
            CompiledPuzzle puzzle,
            PuzzleState state)
        {
            if (puzzle == null)
            {
                throw new ArgumentNullException(nameof(puzzle));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.CellCount != puzzle.Definition.CellCount)
            {
                throw new ArgumentException(
                    "Puzzle state size must match puzzle cell count.",
                    nameof(state));
            }

            var districtCounts = new int[puzzle.DistrictCount];
            var horizontalCounts = new int[puzzle.HorizontalSegments.Length];
            var verticalCounts = new int[puzzle.VerticalSegments.Length];
            var violations = new List<RuleViolation>();

            for (var cellIndex = 0; cellIndex < state.CellCount; cellIndex++)
            {
                var playerState = state[cellIndex];
                if (playerState == PlayerCellState.Empty)
                {
                    continue;
                }

                var definitionCell = puzzle.Definition.Cells[cellIndex];
                if (definitionCell.Kind != CellKind.District)
                {
                    violations.Add(new RuleViolation(
                        RuleViolationCode.StateOnBlockedCell,
                        "Player state cannot be placed on a house or wall.",
                        cellIndex));
                    continue;
                }

                if (playerState != PlayerCellState.Lantern)
                {
                    continue;
                }

                var districtIndex = puzzle.CellToDistrictIndex[cellIndex];
                var horizontalSegment = puzzle.CellToHorizontalSegment[cellIndex];
                var verticalSegment = puzzle.CellToVerticalSegment[cellIndex];

                districtCounts[districtIndex]++;
                horizontalCounts[horizontalSegment]++;
                verticalCounts[verticalSegment]++;
            }

            for (var districtIndex = 0; districtIndex < districtCounts.Length; districtIndex++)
            {
                if (districtCounts[districtIndex] > 1)
                {
                    violations.Add(new RuleViolation(
                        RuleViolationCode.DistrictOverfilled,
                        "A district contains more than one lantern.",
                        relatedIndex: puzzle.DistrictIds[districtIndex]));
                }
            }

            AddVisibilityViolations(horizontalCounts, true, violations);
            AddVisibilityViolations(verticalCounts, false, violations);

            var houseEvaluations = new HouseEvaluation[puzzle.Houses.Length];
            for (var houseIndex = 0; houseIndex < puzzle.Houses.Length; houseIndex++)
            {
                var house = puzzle.Houses[houseIndex];
                var mask = LightDirectionMask.None;

                if (IsOccupied(verticalCounts, house.UpSegmentId))
                    mask |= LightDirectionMask.Up;
                if (IsOccupied(horizontalCounts, house.RightSegmentId))
                    mask |= LightDirectionMask.Right;
                if (IsOccupied(verticalCounts, house.DownSegmentId))
                    mask |= LightDirectionMask.Down;
                if (IsOccupied(horizontalCounts, house.LeftSegmentId))
                    mask |= LightDirectionMask.Left;

                var incomingCount = CountBits(mask);
                houseEvaluations[houseIndex] = new HouseEvaluation(
                    house.CellIndex,
                    house.Target,
                    mask,
                    incomingCount);

                if (incomingCount > house.Target)
                {
                    violations.Add(new RuleViolation(
                        RuleViolationCode.HouseOverlit,
                        "A numbered house currently receives too many light directions.",
                        house.CellIndex));
                }
            }

            var isSolved = violations.Count == 0;

            if (isSolved)
            {
                for (var i = 0; i < districtCounts.Length; i++)
                {
                    if (districtCounts[i] != 1)
                    {
                        isSolved = false;
                        break;
                    }
                }
            }

            if (isSolved)
            {
                for (var i = 0; i < houseEvaluations.Length; i++)
                {
                    if (!houseEvaluations[i].IsSatisfied)
                    {
                        isSolved = false;
                        break;
                    }
                }
            }

            return new PuzzleEvaluationResult(
                districtCounts,
                horizontalCounts,
                verticalCounts,
                houseEvaluations,
                violations,
                isSolved);
        }

        private static void AddVisibilityViolations(
            int[] segmentCounts,
            bool horizontal,
            List<RuleViolation> violations)
        {
            for (var segmentIndex = 0; segmentIndex < segmentCounts.Length; segmentIndex++)
            {
                if (segmentCounts[segmentIndex] > 1)
                {
                    violations.Add(new RuleViolation(
                        RuleViolationCode.LanternVisibility,
                        horizontal
                            ? "Two or more lanterns see each other horizontally."
                            : "Two or more lanterns see each other vertically.",
                        relatedIndex: segmentIndex));
                }
            }
        }

        private static bool IsOccupied(int[] counts, int segmentId)
        {
            return segmentId >= 0 && counts[segmentId] > 0;
        }

        private static int CountBits(LightDirectionMask mask)
        {
            var value = (byte)mask;
            var count = 0;

            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }

            return count;
        }
    }
}
