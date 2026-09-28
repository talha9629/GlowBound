using System;
using System.Collections.Generic;

namespace Glowbound.Core
{
    public sealed class VisibilitySegment
    {
        public int Id { get; }
        public bool IsHorizontal { get; }
        public int[] CellIndices { get; }

        internal VisibilitySegment(int id, bool isHorizontal, int[] cellIndices)
        {
            Id = id;
            IsHorizontal = isHorizontal;
            CellIndices = cellIndices ?? Array.Empty<int>();
        }
    }

    public sealed class HouseConstraint
    {
        public int CellIndex { get; }
        public byte Target { get; }
        public int UpSegmentId { get; }
        public int RightSegmentId { get; }
        public int DownSegmentId { get; }
        public int LeftSegmentId { get; }

        internal HouseConstraint(
            int cellIndex,
            byte target,
            int upSegmentId,
            int rightSegmentId,
            int downSegmentId,
            int leftSegmentId)
        {
            CellIndex = cellIndex;
            Target = target;
            UpSegmentId = upSegmentId;
            RightSegmentId = rightSegmentId;
            DownSegmentId = downSegmentId;
            LeftSegmentId = leftSegmentId;
        }

        public int GetSegmentId(LightDirection direction)
        {
            switch (direction)
            {
                case LightDirection.Up: return UpSegmentId;
                case LightDirection.Right: return RightSegmentId;
                case LightDirection.Down: return DownSegmentId;
                case LightDirection.Left: return LeftSegmentId;
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }
    }

    public sealed class CompiledPuzzle
    {
        private readonly Dictionary<int, int> _districtIdToIndex;

        public PuzzleDefinition Definition { get; }
        public int[] DistrictIds { get; }
        public int[][] DistrictCells { get; }
        public int[] CellToDistrictIndex { get; }
        public int[] CellToHorizontalSegment { get; }
        public int[] CellToVerticalSegment { get; }
        public VisibilitySegment[] HorizontalSegments { get; }
        public VisibilitySegment[] VerticalSegments { get; }
        public HouseConstraint[] Houses { get; }

        public int DistrictCount
        {
            get { return DistrictIds.Length; }
        }

        internal CompiledPuzzle(
            PuzzleDefinition definition,
            int[] districtIds,
            int[][] districtCells,
            int[] cellToDistrictIndex,
            int[] cellToHorizontalSegment,
            int[] cellToVerticalSegment,
            VisibilitySegment[] horizontalSegments,
            VisibilitySegment[] verticalSegments,
            HouseConstraint[] houses)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            DistrictIds = districtIds ?? Array.Empty<int>();
            DistrictCells = districtCells ?? Array.Empty<int[]>();
            CellToDistrictIndex = cellToDistrictIndex ?? Array.Empty<int>();
            CellToHorizontalSegment = cellToHorizontalSegment ?? Array.Empty<int>();
            CellToVerticalSegment = cellToVerticalSegment ?? Array.Empty<int>();
            HorizontalSegments = horizontalSegments ?? Array.Empty<VisibilitySegment>();
            VerticalSegments = verticalSegments ?? Array.Empty<VisibilitySegment>();
            Houses = houses ?? Array.Empty<HouseConstraint>();

            _districtIdToIndex = new Dictionary<int, int>(DistrictIds.Length);
            for (var i = 0; i < DistrictIds.Length; i++)
            {
                _districtIdToIndex.Add(DistrictIds[i], i);
            }
        }

        public bool TryGetDistrictIndex(int districtId, out int districtIndex)
        {
            return _districtIdToIndex.TryGetValue(districtId, out districtIndex);
        }

        public int GetDistrictIndex(int districtId)
        {
            if (!_districtIdToIndex.TryGetValue(districtId, out var districtIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(districtId));
            }

            return districtIndex;
        }
    }
}
