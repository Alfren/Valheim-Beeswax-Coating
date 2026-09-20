namespace BeeswaxCoating
{
    internal enum WaxOutcome
    {
        NoTarget,
        NotBuildingPiece,
        DoesNotWeather,
        AlreadyWaxed,
        NotEnoughCoatings,
        Applied
    }

    /// <summary>
    /// Pure decision logic for coating a building piece. Kept free of Unity/game
    /// types so it can be unit tested directly (tests compile this same file).
    /// </summary>
    internal static class WaxDecision
    {
        public static WaxOutcome Evaluate(
            bool hasHoveredPiece,
            bool hasWearNTear,
            bool suffersWeatherWear,
            bool alreadyWaxed,
            int coatingsOwned,
            int coatingsRequired)
        {
            if (!hasHoveredPiece)
            {
                return WaxOutcome.NoTarget;
            }
            if (!hasWearNTear)
            {
                return WaxOutcome.NotBuildingPiece;
            }
            if (!suffersWeatherWear)
            {
                return WaxOutcome.DoesNotWeather;
            }
            if (alreadyWaxed)
            {
                return WaxOutcome.AlreadyWaxed;
            }
            if (coatingsOwned < coatingsRequired)
            {
                return WaxOutcome.NotEnoughCoatings;
            }
            return WaxOutcome.Applied;
        }
    }
}
