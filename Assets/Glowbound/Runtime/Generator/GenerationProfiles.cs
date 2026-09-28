namespace Glowbound.Core.Generation
{
    /// <summary>Construction baselines proven by seed stress tests; not difficulty ratings.</summary>
    public static class GenerationProfiles
    {
        public static GenerationSettings Board5x5()
        {
            return new GenerationSettings
            {
                Width = 5, Height = 5, DistrictCount = 5,
                BlockerCount = 5, HouseFraction = 0.70f,
                MinimumHouseCount = 2, MaxAttempts = 300
            };
        }

        public static GenerationSettings Board6x6()
        {
            return new GenerationSettings
            {
                Width = 6, Height = 6, DistrictCount = 6,
                BlockerCount = 7, HouseFraction = 0.75f,
                MinimumHouseCount = 3, MaxAttempts = 400
            };
        }

        public static GenerationSettings Board7x7()
        {
            return new GenerationSettings
            {
                Width = 7, Height = 7, DistrictCount = 7,
                BlockerCount = 12, HouseFraction = 0.80f,
                MinimumHouseCount = 3, MaxAttempts = 300
            };
        }

        public static GenerationSettings Board8x8()
        {
            return new GenerationSettings
            {
                Width = 8, Height = 8, DistrictCount = 8,
                BlockerCount = 16, HouseFraction = 0.80f,
                MinimumHouseCount = 4, MaxAttempts = 300
            };
        }
    }
}
