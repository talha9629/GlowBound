using Glowbound.Core;
using UnityEngine;

namespace Glowbound.Unity
{
    [CreateAssetMenu(fileName = "Puzzle", menuName = "Glowbound/Puzzle Definition", order = 0)]
    public sealed class PuzzleAsset : ScriptableObject
    {
        [SerializeField] private string puzzleId = string.Empty;
        [SerializeField, Min(1)] private int width = 5;
        [SerializeField, Min(1)] private int height = 5;
        [SerializeField] private CellDefinition[] cells = System.Array.Empty<CellDefinition>();

        [Header("Authoring Metadata")]
        [SerializeField] private int generatorSeed;
        [SerializeField] private string generationProfile = string.Empty;
        [SerializeField] private int generationAttempts;
        [SerializeField] private long solverNodes;
        [SerializeField] private long solverBacktracks;
        [SerializeField] private PuzzleDifficultyLabel difficulty = PuzzleDifficultyLabel.Unrated;
        [SerializeField, Min(0)] private int difficultyScore;
        [SerializeField, Min(0)] private int campaignOrder;
        [SerializeField] private int[] knownSolutionLanternCells = System.Array.Empty<int>();

        public string PuzzleId => puzzleId;
        public int Width => width;
        public int Height => height;
        public int GeneratorSeed => generatorSeed;
        public string GenerationProfile => generationProfile;
        public int GenerationAttempts => generationAttempts;
        public long SolverNodes => solverNodes;
        public long SolverBacktracks => solverBacktracks;
        public PuzzleDifficultyLabel Difficulty => difficulty;
        public int DifficultyScore => difficultyScore;
        public int CampaignOrder => campaignOrder;
        public int[] CopyKnownSolution() => knownSolutionLanternCells == null
            ? System.Array.Empty<int>()
            : (int[])knownSolutionLanternCells.Clone();

        public PuzzleDefinition CreateDefinition()
        {
            var cloned = cells == null
                ? System.Array.Empty<CellDefinition>()
                : (CellDefinition[])cells.Clone();
            return new PuzzleDefinition(puzzleId, width, height, cloned);
        }

#if UNITY_EDITOR
        public void SetGeneratedData(PuzzleDefinition definition, int seed, string profile,
            int attempts, long nodes, long backtracks, int[] knownSolution)
        {
            puzzleId = definition.Id;
            width = definition.Width;
            height = definition.Height;
            cells = (CellDefinition[])definition.Cells.Clone();
            generatorSeed = seed;
            generationProfile = profile ?? string.Empty;
            generationAttempts = attempts;
            solverNodes = nodes;
            solverBacktracks = backtracks;
            difficulty = PuzzleDifficultyLabel.Unrated;
            difficultyScore = 0;
            knownSolutionLanternCells = knownSolution == null
                ? System.Array.Empty<int>()
                : (int[])knownSolution.Clone();
        }

        private void OnValidate()
        {
            if (width < 1) width = 1;
            if (height < 1) height = 1;
            var required = width * height;
            if (cells == null || cells.Length != required)
                System.Array.Resize(ref cells, required);
        }
#endif
    }
}
