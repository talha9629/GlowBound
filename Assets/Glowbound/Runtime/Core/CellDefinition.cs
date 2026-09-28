using System;

namespace Glowbound.Core
{
    [Serializable]
    public struct CellDefinition
    {
        public const int NoDistrict = -1;

        public CellKind Kind;
        public int DistrictId;
        public byte HouseTarget;

        public static CellDefinition District(int districtId)
        {
            return new CellDefinition
            {
                Kind = CellKind.District,
                DistrictId = districtId,
                HouseTarget = 0
            };
        }

        public static CellDefinition House(byte target)
        {
            return new CellDefinition
            {
                Kind = CellKind.House,
                DistrictId = NoDistrict,
                HouseTarget = target
            };
        }

        public static CellDefinition Wall()
        {
            return new CellDefinition
            {
                Kind = CellKind.Wall,
                DistrictId = NoDistrict,
                HouseTarget = 0
            };
        }
    }
}
