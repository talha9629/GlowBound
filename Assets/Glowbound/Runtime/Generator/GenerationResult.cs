using System;

namespace Glowbound.Core.Generation
{
    public sealed class GenerationDiagnostics
    {
        public int Attempts;
        public int RejectedDistricts;
        public int RejectedSolutions;
        public int RejectedDefinitions;
        public int RejectedHouseCount;
        public int RejectedNoSolution;
        public int RejectedMultipleSolutions;
        public long LastSolverNodes;
        public long LastSolverBacktracks;
    }

    public sealed class GenerationResult
    {
        public bool Success { get; }
        public int Seed { get; }
        public PuzzleDefinition Definition { get; }
        public int[] KnownSolutionLanternCells { get; }
        public PuzzleSolveResult SolveResult { get; }
        public GenerationDiagnostics Diagnostics { get; }
        public string FailureReason { get; }

        internal GenerationResult(bool success, int seed, PuzzleDefinition definition,
            int[] knownSolution, PuzzleSolveResult solveResult,
            GenerationDiagnostics diagnostics, string failureReason)
        {
            Success = success;
            Seed = seed;
            Definition = definition;
            KnownSolutionLanternCells = knownSolution ?? Array.Empty<int>();
            SolveResult = solveResult;
            Diagnostics = diagnostics ?? new GenerationDiagnostics();
            FailureReason = failureReason ?? string.Empty;
        }
    }
}
