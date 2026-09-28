using System;
using Glowbound.Core;

namespace Glowbound.Core.Tests
{
    internal static class TestPuzzleFactory
    {
        public static PuzzleDefinition Parse(params string[] rows)
        {
            if (rows == null || rows.Length == 0)
            {
                throw new ArgumentException("At least one row is required.", nameof(rows));
            }

            var width = rows[0].Length;
            if (width == 0)
            {
                throw new ArgumentException("Rows cannot be empty.", nameof(rows));
            }

            for (var y = 1; y < rows.Length; y++)
            {
                if (rows[y].Length != width)
                {
                    throw new ArgumentException("All rows must have the same width.", nameof(rows));
                }
            }

            var cells = new CellDefinition[width * rows.Length];

            for (var y = 0; y < rows.Length; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = (y * width) + x;
                    var symbol = rows[y][x];

                    if (symbol >= 'A' && symbol <= 'Z')
                    {
                        cells[index] = CellDefinition.District(symbol - 'A');
                    }
                    else if (symbol >= '1' && symbol <= '3')
                    {
                        cells[index] = CellDefinition.House((byte)(symbol - '0'));
                    }
                    else if (symbol == '#')
                    {
                        cells[index] = CellDefinition.Wall();
                    }
                    else
                    {
                        throw new ArgumentException(
                            "Unsupported test puzzle symbol: " + symbol,
                            nameof(rows));
                    }
                }
            }

            return new PuzzleDefinition(
                "test",
                width,
                rows.Length,
                cells);
        }
    }
}
