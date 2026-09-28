using Glowbound.Core.Generation;
using NUnit.Framework;

namespace Glowbound.Core.Tests
{
    public sealed class PuzzleGeneratorTests
    {
        [Test]
        public void GenerationRandom_SameSeedProducesSameSequence()
        {
            var a = new GenerationRandom(12345);
            var b = new GenerationRandom(12345);
            for (var i = 0; i < 32; i++)
                Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void DistrictGenerator_ProducesConnectedValidDistricts()
        {
            var random = new GenerationRandom(77);
            var map = DistrictGenerator.Generate(6, 6, 6, random);
            var cells = new CellDefinition[map.Length];
            for (var i = 0; i < map.Length; i++)
                cells[i] = CellDefinition.District(map[i]);
            var definition = new PuzzleDefinition("district-test", 6, 6, cells);
            Assert.IsTrue(PuzzleDefinitionValidator.Validate(definition).IsValid);
        }

        [Test]
        public void PuzzleGenerator_SameSeedProducesIdenticalDefinition()
        {
            var settings = new GenerationSettings();
            var a = PuzzleGenerator.Generate(123, settings);
            var b = PuzzleGenerator.Generate(123, settings);
            Assert.IsTrue(a.Success);
            Assert.IsTrue(b.Success);
            AssertDefinitionsEqual(a.Definition, b.Definition);
            CollectionAssert.AreEqual(a.KnownSolutionLanternCells, b.KnownSolutionLanternCells);
        }

        [Test]
        public void PuzzleGenerator_ResultIsUniqueAndKnownSolutionSolves()
        {
            var result = PuzzleGenerator.Generate(42, new GenerationSettings());
            Assert.IsTrue(result.Success, result.FailureReason);
            Assert.AreEqual(SolutionMultiplicity.Unique, result.SolveResult.Multiplicity);

            var compiled = PuzzleCompiler.Compile(result.Definition);
            var state = new PuzzleState(result.Definition);
            foreach (var cell in result.KnownSolutionLanternCells)
                state[cell] = PlayerCellState.Lantern;
            Assert.IsTrue(PuzzleEvaluator.Evaluate(compiled, state).IsSolved);
        }

        [Test]
        public void PuzzleGenerator_SeedSampleAlwaysReturnsUniquePuzzle()
        {
            var settings = new GenerationSettings { MaxAttempts = 300 };
            for (var seed = 1; seed <= 10; seed++)
            {
                var result = PuzzleGenerator.Generate(seed, settings);
                Assert.IsTrue(result.Success, $"Seed {seed}: {result.FailureReason}");
                Assert.AreEqual(SolutionMultiplicity.Unique, result.SolveResult.Multiplicity, $"Seed {seed}");
            }
        }

        private static void AssertDefinitionsEqual(PuzzleDefinition a, PuzzleDefinition b)
        {
            Assert.AreEqual(a.Width, b.Width);
            Assert.AreEqual(a.Height, b.Height);
            Assert.AreEqual(a.Cells.Length, b.Cells.Length);
            for (var i = 0; i < a.Cells.Length; i++)
            {
                Assert.AreEqual(a.Cells[i].Kind, b.Cells[i].Kind, $"Kind at {i}");
                Assert.AreEqual(a.Cells[i].DistrictId, b.Cells[i].DistrictId, $"District at {i}");
                Assert.AreEqual(a.Cells[i].HouseTarget, b.Cells[i].HouseTarget, $"House target at {i}");
            }
        }
    }
}
