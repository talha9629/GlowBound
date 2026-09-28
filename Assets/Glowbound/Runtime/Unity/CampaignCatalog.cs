using System;
using UnityEngine;

namespace Glowbound.Unity
{
    [Serializable]
    public struct CampaignLevelEntry
    {
        public PuzzleAsset Puzzle;
        public PuzzleDifficultyLabel DisplayDifficulty;
        [Tooltip("Optional designer note; not shown to player by default.")]
        public string Note;
    }

    [CreateAssetMenu(fileName = "CampaignCatalog", menuName = "Glowbound/Campaign Catalog", order = 1)]
    public sealed class CampaignCatalog : ScriptableObject
    {
        [SerializeField] private CampaignLevelEntry[] levels = Array.Empty<CampaignLevelEntry>();

        public int Count => levels == null ? 0 : levels.Length;
        public CampaignLevelEntry GetLevel(int zeroBasedIndex) => levels[zeroBasedIndex];
        public CampaignLevelEntry[] CopyLevels() => levels == null
            ? Array.Empty<CampaignLevelEntry>()
            : (CampaignLevelEntry[])levels.Clone();
    }
}
