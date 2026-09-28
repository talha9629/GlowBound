using System;

namespace Glowbound.Core.Generation
{
    public static class PuzzleGenerator
    {
        public static GenerationResult Generate(int seed, GenerationSettings settings = null)
        {
            settings = settings?.Clone() ?? new GenerationSettings();
            settings.Validate();
            var random = new GenerationRandom(seed);
            var diagnostics = new GenerationDiagnostics();

            for (var attempt = 1; attempt <= settings.MaxAttempts; attempt++)
            {
                diagnostics.Attempts = attempt;
                var districtMap = DistrictGenerator.Generate(
                    settings.Width, settings.Height, settings.DistrictCount, random);

                if (!SolutionGenerator.TryGenerate(
                    settings.Width, settings.Height, settings.DistrictCount,
                    districtMap, random, settings.MaxSolutionSearchNodes, out var knownSolution))
                {
                    diagnostics.RejectedSolutions++;
                    continue;
                }

                if (!BlockerGenerator.TryGenerate(
                    settings.Width, settings.Height, settings.DistrictCount,
                    districtMap, knownSolution, settings.BlockerCount, random, out var blocked))
                {
                    diagnostics.RejectedDistricts++;
                    continue;
                }
                var id = $"gen-{seed}-{attempt}";
                if (!ClueDeriver.TryBuildDefinition(
                    id, settings.Width, settings.Height, settings.DistrictCount,
                    districtMap, knownSolution, blocked, settings, random,
                    out var definition, out var houseCount))
                {
                    diagnostics.RejectedHouseCount++;
                    continue;
                }

                var validation = PuzzleDefinitionValidator.Validate(definition);
                if (!validation.IsValid)
                {
                    diagnostics.RejectedDefinitions++;
                    continue;
                }

                var compiled = PuzzleCompiler.Compile(definition);
                if (!KnownSolutionIsSolved(compiled, knownSolution))
                {
                    diagnostics.RejectedDefinitions++;
                    continue;
                }

                var solve = PuzzleSolver.Analyze(compiled, 2);
                diagnostics.LastSolverNodes = solve.NodesVisited;
                diagnostics.LastSolverBacktracks = solve.Backtracks;
                if (solve.Multiplicity == SolutionMultiplicity.NoSolution)
                {
                    diagnostics.RejectedNoSolution++;
                    continue;
                }
                if (solve.Multiplicity == SolutionMultiplicity.Multiple)
                {
                    diagnostics.RejectedMultipleSolutions++;
                    continue;
                }

                return new GenerationResult(true, seed, definition,
                    knownSolution, solve, diagnostics, string.Empty);
            }
            return new GenerationResult(false, seed, null, Array.Empty<int>(), null,
                diagnostics, $"No unique puzzle found in {settings.MaxAttempts} attempts.");
        }

        private static bool KnownSolutionIsSolved(CompiledPuzzle puzzle, int[] lanternCells)
        {
            var state = new PuzzleState(puzzle.Definition);
            for (var i = 0; i < lanternCells.Length; i++)
            {
                var cell = lanternCells[i];
                if (cell < 0 || cell >= state.CellCount) return false;
                state[cell] = PlayerCellState.Lantern;
            }
            return PuzzleEvaluator.Evaluate(puzzle, state).IsSolved;
        }
    }
}
