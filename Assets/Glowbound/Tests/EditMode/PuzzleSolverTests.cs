using Glowbound.Core;
using NUnit.Framework;

namespace Glowbound.Core.Tests
{
    public sealed class PuzzleSolverTests
    {
        [Test]
        public void Solver_SingleCellPuzzle_IsUnique()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse("A"));

            var result = PuzzleSolver.Analyze(puzzle);

            Assert.That(result.Multiplicity, Is.EqualTo(SolutionMultiplicity.Unique));
            Assert.That(result.SolutionCount, Is.EqualTo(1));
            Assert.That(result.FirstSolutionLanternCells, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Solver_VisibleSingletonDistricts_HaveNoSolution()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse("AB"));

            var result = PuzzleSolver.Analyze(puzzle);

            Assert.That(result.Multiplicity, Is.EqualTo(SolutionMultiplicity.NoSolution));
            Assert.That(result.SolutionCount, Is.EqualTo(0));
        }

        [Test]
        public void Solver_TwoByTwoDistrictRows_HaveMultipleSolutions()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse(
                "AA",
                "BB"));

            var result = PuzzleSolver.Analyze(puzzle);

            Assert.That(result.Multiplicity, Is.EqualTo(SolutionMultiplicity.Multiple));
            Assert.That(result.SolutionCount, Is.EqualTo(2));
        }

        [Test]
        public void Solver_WallSeparatedSingletons_AreUnique()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse("A#B"));

            var result = PuzzleSolver.Analyze(puzzle);

            Assert.That(result.Multiplicity, Is.EqualTo(SolutionMultiplicity.Unique));
            Assert.That(result.SolutionCount, Is.EqualTo(1));
        }

        [Test]
        public void Solver_HouseTwoBetweenSingletons_IsUnique()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse("A2B"));

            var result = PuzzleSolver.Analyze(puzzle);

            Assert.That(result.Multiplicity, Is.EqualTo(SolutionMultiplicity.Unique));
            Assert.That(result.SolutionCount, Is.EqualTo(1));
        }

        [Test]
        public void Solver_HouseOneBetweenForcedSingletons_HasNoSolution()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse("A1B"));

            var result = PuzzleSolver.Analyze(puzzle);

            Assert.That(result.Multiplicity, Is.EqualTo(SolutionMultiplicity.NoSolution));
            Assert.That(result.SolutionCount, Is.EqualTo(0));
        }

        [Test]
        public void Solver_ResultSolution_SatisfiesCanonicalEvaluator()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse(
                "#A#",
                "B3C",
                "###"));

            var result = PuzzleSolver.Analyze(puzzle);
            var state = new PuzzleState(puzzle.Definition);

            for (var i = 0; i < result.FirstSolutionLanternCells.Length; i++)
            {
                state[result.FirstSolutionLanternCells[i]] = PlayerCellState.Lantern;
            }

            var evaluation = PuzzleEvaluator.Evaluate(puzzle, state);

            Assert.That(result.Multiplicity, Is.EqualTo(SolutionMultiplicity.Unique));
            Assert.That(evaluation.IsSolved, Is.True);
        }
    }
}
