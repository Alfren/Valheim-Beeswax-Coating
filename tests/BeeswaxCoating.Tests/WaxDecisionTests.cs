using BeeswaxCoating;
using Xunit;

namespace BeeswaxCoating.Tests
{
    public class WaxDecisionTests
    {
        [Fact]
        public void NoPieceHovered_IsNoTarget_RegardlessOfOtherInputs()
        {
            Assert.Equal(WaxOutcome.NoTarget, WaxDecision.Evaluate(false, false, false, false, 0, 1));
            Assert.Equal(WaxOutcome.NoTarget, WaxDecision.Evaluate(false, true, true, false, 99, 1));
        }

        [Fact]
        public void PieceWithoutWearNTear_IsNotBuildingPiece()
        {
            Assert.Equal(WaxOutcome.NotBuildingPiece, WaxDecision.Evaluate(true, false, false, false, 99, 1));
        }

        [Fact]
        public void NonWeatheringPiece_DoesNotWeather()
        {
            Assert.Equal(WaxOutcome.DoesNotWeather, WaxDecision.Evaluate(true, true, false, false, 99, 1));
        }

        [Fact]
        public void AlreadyWaxedPiece_IsRejected_EvenWithoutCoatings()
        {
            Assert.Equal(WaxOutcome.AlreadyWaxed, WaxDecision.Evaluate(true, true, true, true, 0, 1));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        [InlineData(0, 10)]
        public void InsufficientCoatings_IsRejected(int owned, int required)
        {
            Assert.Equal(WaxOutcome.NotEnoughCoatings, WaxDecision.Evaluate(true, true, true, false, owned, required));
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(5, 1)]
        [InlineData(10, 10)]
        public void EnoughCoatings_Applies(int owned, int required)
        {
            Assert.Equal(WaxOutcome.Applied, WaxDecision.Evaluate(true, true, true, false, owned, required));
        }

        [Fact]
        public void GuardOrdering_MissingCoatingsCheckedAfterWaxState()
        {
            // A waxed piece reports AlreadyWaxed, not NotEnoughCoatings
            Assert.Equal(WaxOutcome.AlreadyWaxed, WaxDecision.Evaluate(true, true, true, true, 0, 5));
        }
    }
}
