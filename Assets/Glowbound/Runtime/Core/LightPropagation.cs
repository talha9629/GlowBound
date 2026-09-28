using System;
using System.Collections.Generic;

namespace Glowbound.Core
{
    public enum LightRayTermination
    {
        BoardEdge,
        Wall,
        House,
        Lantern
    }

    public sealed class LightRayTrace
    {
        public LightDirection Direction { get; }
        public int[] TraversedPlayableCells { get; }
        public int TerminalCellIndex { get; }
        public LightRayTermination Termination { get; }

        public bool IlluminatesHouse
        {
            get { return Termination == LightRayTermination.House; }
        }

        public bool HitsLantern
        {
            get { return Termination == LightRayTermination.Lantern; }
        }

        internal LightRayTrace(
            LightDirection direction,
            int[] traversedPlayableCells,
            int terminalCellIndex,
            LightRayTermination termination)
        {
            Direction = direction;
            TraversedPlayableCells = traversedPlayableCells ?? Array.Empty<int>();
            TerminalCellIndex = terminalCellIndex;
            Termination = termination;
        }
    }

    public static class LightPropagation
    {
        public static LightRayTrace[] TraceFromLantern(
            CompiledPuzzle puzzle,
            PuzzleState state,
            int lanternCellIndex)
        {
            ValidateTraceOrigin(puzzle, state, lanternCellIndex);

            return new[]
            {
                TraceDirection(puzzle, state, lanternCellIndex, LightDirection.Up),
                TraceDirection(puzzle, state, lanternCellIndex, LightDirection.Right),
                TraceDirection(puzzle, state, lanternCellIndex, LightDirection.Down),
                TraceDirection(puzzle, state, lanternCellIndex, LightDirection.Left)
            };
        }

        public static LightRayTrace TraceDirection(
            CompiledPuzzle puzzle,
            PuzzleState state,
            int lanternCellIndex,
            LightDirection direction)
        {
            ValidateTraceOrigin(puzzle, state, lanternCellIndex);

            puzzle.Definition.ToCoordinates(lanternCellIndex, out var x, out var y);
            GetStep(direction, out var dx, out var dy);

            var traversed = new List<int>();
            x += dx;
            y += dy;

            while (puzzle.Definition.IsInBounds(x, y))
            {
                var index = puzzle.Definition.ToIndex(x, y);
                var cell = puzzle.Definition.Cells[index];

                if (cell.Kind == CellKind.Wall)
                {
                    return new LightRayTrace(
                        direction,
                        traversed.ToArray(),
                        index,
                        LightRayTermination.Wall);
                }

                if (cell.Kind == CellKind.House)
                {
                    return new LightRayTrace(
                        direction,
                        traversed.ToArray(),
                        index,
                        LightRayTermination.House);
                }

                if (state[index] == PlayerCellState.Lantern)
                {
                    return new LightRayTrace(
                        direction,
                        traversed.ToArray(),
                        index,
                        LightRayTermination.Lantern);
                }

                traversed.Add(index);
                x += dx;
                y += dy;
            }

            return new LightRayTrace(
                direction,
                traversed.ToArray(),
                -1,
                LightRayTermination.BoardEdge);
        }

        private static void ValidateTraceOrigin(
            CompiledPuzzle puzzle,
            PuzzleState state,
            int lanternCellIndex)
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

            if (lanternCellIndex < 0 || lanternCellIndex >= state.CellCount)
            {
                throw new ArgumentOutOfRangeException(nameof(lanternCellIndex));
            }

            if (puzzle.Definition.Cells[lanternCellIndex].Kind != CellKind.District ||
                state[lanternCellIndex] != PlayerCellState.Lantern)
            {
                throw new InvalidOperationException(
                    "Trace origin must be a lantern on a playable district cell.");
            }
        }

        private static void GetStep(
            LightDirection direction,
            out int dx,
            out int dy)
        {
            switch (direction)
            {
                case LightDirection.Up:
                    dx = 0;
                    dy = -1;
                    break;
                case LightDirection.Right:
                    dx = 1;
                    dy = 0;
                    break;
                case LightDirection.Down:
                    dx = 0;
                    dy = 1;
                    break;
                case LightDirection.Left:
                    dx = -1;
                    dy = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }
}
