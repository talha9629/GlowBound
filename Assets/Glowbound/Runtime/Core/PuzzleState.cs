using System;

namespace Glowbound.Core
{
    public enum PlayerCellState : byte
    {
        Empty = 0,
        X = 1,
        Lantern = 2
    }

    public sealed class PuzzleState
    {
        private readonly PlayerCellState[] _cells;

        public int CellCount
        {
            get { return _cells.Length; }
        }

        public PuzzleState(int cellCount)
        {
            if (cellCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cellCount));
            }

            _cells = new PlayerCellState[cellCount];
        }

        public PuzzleState(PuzzleDefinition definition)
            : this(definition == null ? throw new ArgumentNullException(nameof(definition)) : definition.CellCount)
        {
        }

        private PuzzleState(PlayerCellState[] cells)
        {
            _cells = cells;
        }

        public PlayerCellState this[int index]
        {
            get { return _cells[index]; }
            set { _cells[index] = value; }
        }

        public void Clear()
        {
            Array.Clear(_cells, 0, _cells.Length);
        }

        public PuzzleState Clone()
        {
            return new PuzzleState((PlayerCellState[])_cells.Clone());
        }

        public PlayerCellState[] CopyCells()
        {
            return (PlayerCellState[])_cells.Clone();
        }
    }
}
