using System;

namespace Glowbound.Core
{
    public enum CellKind : byte
    {
        District = 0,
        House = 1,
        Wall = 2
    }

    public enum LightDirection : byte
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    [Flags]
    public enum LightDirectionMask : byte
    {
        None = 0,
        Up = 1 << 0,
        Right = 1 << 1,
        Down = 1 << 2,
        Left = 1 << 3
    }
}
