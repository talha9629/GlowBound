using System;

namespace Glowbound.Core.Generation
{
    [Serializable]
    public sealed class GenerationSettings
    {
        public int Width = 5;
        public int Height = 5;
        public int DistrictCount = 5;
        public int BlockerCount = 5;
        public float HouseFraction = 0.70f;
        public int MinimumHouseCount = 2;
        public int MaxAttempts = 300;
        public int MaxSolutionSearchNodes = 50000;

        public GenerationSettings Clone()
        {
            return (GenerationSettings)MemberwiseClone();
        }

        public void Validate()
        {
            if (Width < 2 || Height < 2) throw new InvalidOperationException("Board must be at least 2x2.");
            if (DistrictCount < 1) throw new InvalidOperationException("DistrictCount must be positive.");
            if (DistrictCount > Width * Height) throw new InvalidOperationException("Too many districts for board cells.");
            if (DistrictCount > Math.Min(Width, Height)) throw new InvalidOperationException("Current known-solution generator requires DistrictCount <= min(width,height).");
            if (BlockerCount < 0 || BlockerCount >= Width * Height) throw new InvalidOperationException("Invalid BlockerCount.");
            if (HouseFraction < 0f || HouseFraction > 1f) throw new InvalidOperationException("HouseFraction must be 0..1.");
            if (MinimumHouseCount < 0) throw new InvalidOperationException("MinimumHouseCount cannot be negative.");
            if (MaxAttempts < 1) throw new InvalidOperationException("MaxAttempts must be positive.");
            if (MaxSolutionSearchNodes < 1) throw new InvalidOperationException("MaxSolutionSearchNodes must be positive.");
        }
    }
}
