using Glowbound.Core;
using NUnit.Framework;

namespace Glowbound.Core.Tests
{
    public sealed class PuzzleCoreTests
    {
        [Test]
        public void Validator_DisconnectedDistrict_IsRejected()
        {
            var puzzle = TestPuzzleFactory.Parse("A#A");

            var result = PuzzleDefinitionValidator.Validate(puzzle);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.HasIssue(PuzzleValidationCode.DisconnectedDistrict), Is.True);
        }

        [Test]
        public void Validator_HouseWithTooFewDirections_IsRejected()
        {
            var puzzle = TestPuzzleFactory.Parse("3A#");

            var result = PuzzleDefinitionValidator.Validate(puzzle);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.HasIssue(PuzzleValidationCode.ImpossibleHouseTarget), Is.True);
        }

        [Test]
        public void Compiler_HouseSplitsVisibilitySegments()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse("A2B"));

            Assert.That(puzzle.HorizontalSegments.Length, Is.EqualTo(2));
            Assert.That(puzzle.Houses.Length, Is.EqualTo(1));
            Assert.That(puzzle.Houses[0].LeftSegmentId, Is.GreaterThanOrEqualTo(0));
            Assert.That(puzzle.Houses[0].RightSegmentId, Is.GreaterThanOrEqualTo(0));
            Assert.That(
                puzzle.Houses[0].LeftSegmentId,
                Is.Not.EqualTo(puzzle.Houses[0].RightSegmentId));
        }

        [Test]
        public void Evaluator_TwoVisibleLanterns_CreateContradiction()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse("AB"));
            var state = new PuzzleState(puzzle.Definition);
            state[0] = PlayerCellState.Lantern;
            state[1] = PlayerCellState.Lantern;

            var result = PuzzleEvaluator.Evaluate(puzzle, state);

            Assert.That(result.HasContradiction, Is.True);
            Assert.That(result.IsSolved, Is.False);
            Assert.That(
                HasViolation(result, RuleViolationCode.LanternVisibility),
                Is.True);
        }

        [Test]
        public void Evaluator_HouseBlocksLanternVisibility_AndCountsBothDirections()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse("A2B"));
            var state = new PuzzleState(puzzle.Definition);
            state[0] = PlayerCellState.Lantern;
            state[2] = PlayerCellState.Lantern;

            var result = PuzzleEvaluator.Evaluate(puzzle, state);

            Assert.That(result.HasContradiction, Is.False);
            Assert.That(result.Houses[0].IncomingCount, Is.EqualTo(2));
            Assert.That(result.Houses[0].IsSatisfied, Is.True);
            Assert.That(result.IsSolved, Is.True);
        }

        [Test]
        public void Evaluator_HouseOneOverlit_IsImmediateContradiction()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse("A1B"));
            var state = new PuzzleState(puzzle.Definition);
            state[0] = PlayerCellState.Lantern;
            state[2] = PlayerCellState.Lantern;

            var result = PuzzleEvaluator.Evaluate(puzzle, state);

            Assert.That(result.HasContradiction, Is.True);
            Assert.That(
                HasViolation(result, RuleViolationCode.HouseOverlit),
                Is.True);
        }

        [Test]
        public void Evaluator_HouseThree_CanReceiveThreeDirections()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse(
                "#A#",
                "B3C",
                "###"));
            var state = new PuzzleState(puzzle.Definition);
            state[1] = PlayerCellState.Lantern;
            state[3] = PlayerCellState.Lantern;
            state[5] = PlayerCellState.Lantern;

            var result = PuzzleEvaluator.Evaluate(puzzle, state);

            Assert.That(result.HasContradiction, Is.False);
            Assert.That(result.Houses[0].IncomingCount, Is.EqualTo(3));
            Assert.That(result.IsSolved, Is.True);
        }

        [Test]
        public void Evaluator_DarkOrdinaryCellsAreAllowed()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse("AAA"));
            var state = new PuzzleState(puzzle.Definition);
            state[0] = PlayerCellState.Lantern;

            var result = PuzzleEvaluator.Evaluate(puzzle, state);

            Assert.That(result.IsSolved, Is.True);
        }

        [Test]
        public void LightPropagation_XMarkDoesNotBlockBeam()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse("AAA"));
            var state = new PuzzleState(puzzle.Definition);
            state[0] = PlayerCellState.Lantern;
            state[1] = PlayerCellState.X;

            var trace = LightPropagation.TraceDirection(
                puzzle,
                state,
                0,
                LightDirection.Right);

            Assert.That(trace.Termination, Is.EqualTo(LightRayTermination.BoardEdge));
            Assert.That(trace.TraversedPlayableCells, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void LightPropagation_WallStopsBeam()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse("A#B"));
            var state = new PuzzleState(puzzle.Definition);
            state[0] = PlayerCellState.Lantern;

            var trace = LightPropagation.TraceDirection(
                puzzle,
                state,
                0,
                LightDirection.Right);

            Assert.That(trace.Termination, Is.EqualTo(LightRayTermination.Wall));
            Assert.That(trace.TerminalCellIndex, Is.EqualTo(1));
            Assert.That(trace.TraversedPlayableCells, Is.Empty);
        }

        [Test]
        public void LightPropagation_HouseReceivesLightAndStopsBeam()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse("A1B"));
            var state = new PuzzleState(puzzle.Definition);
            state[0] = PlayerCellState.Lantern;

            var trace = LightPropagation.TraceDirection(
                puzzle,
                state,
                0,
                LightDirection.Right);

            Assert.That(trace.Termination, Is.EqualTo(LightRayTermination.House));
            Assert.That(trace.IlluminatesHouse, Is.True);
            Assert.That(trace.TerminalCellIndex, Is.EqualTo(1));
        }

        [Test]
        public void LightPropagation_DistrictBoundaryDoesNotBlockBeam()
        {
            var puzzle = PuzzleCompiler.Compile(TestPuzzleFactory.Parse("ABC"));
            var state = new PuzzleState(puzzle.Definition);
            state[0] = PlayerCellState.Lantern;
            state[2] = PlayerCellState.Lantern;

            var trace = LightPropagation.TraceDirection(
                puzzle,
                state,
                0,
                LightDirection.Right);

            Assert.That(trace.Termination, Is.EqualTo(LightRayTermination.Lantern));
            Assert.That(trace.TerminalCellIndex, Is.EqualTo(2));
            Assert.That(trace.TraversedPlayableCells, Is.EqualTo(new[] { 1 }));
        }

        private static bool HasViolation(
            PuzzleEvaluationResult result,
            RuleViolationCode code)
        {
            for (var i = 0; i < result.Violations.Count; i++)
            {
                if (result.Violations[i].Code == code)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
