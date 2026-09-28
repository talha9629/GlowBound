using System;

namespace Glowbound.Core
{
    [Serializable]
    public sealed class PuzzleDefinition
    {
        public string Id = string.Empty;
        public int Width;
        public int Height;
        public CellDefinition[] Cells = Array.Empty<CellDefinition>();

        public int CellCount
        {
            get { return Width > 0 && Height > 0 ? Width * Height : 0; }
        }

        public PuzzleDefinition()
        {
        }

        public PuzzleDefinition(string id, int width, int height, CellDefinition[] cells)
        {
            Id = id ?? string.Empty;
            Width = width;
            Height = height;
            Cells = cells ?? Array.Empty<CellDefinition>();
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public int ToIndex(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                throw new ArgumentOutOfRangeException(nameof(x), "Coordinate is outside the puzzle.");
            }

            return (y * Width) + x;
        }

        public void ToCoordinates(int index, out int x, out int y)
        {
            if (index < 0 || index >= CellCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            x = index % Width;
            y = index / Width;
        }

        public CellDefinition GetCell(int x, int y)
        {
            return Cells[ToIndex(x, y)];
        }
    }
}
