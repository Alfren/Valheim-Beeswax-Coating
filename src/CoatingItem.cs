namespace BeeswaxCoating
{
    internal static class CoatingItem
    {
        public const string PrefabName = "BeeswaxCoating";

        // Legacy item kept registered so existing worlds/inventories stay valid.
        // Nothing produces or consumes it - the coating crafts from honey and resin.
        public const string WaxPrefabName = "Beeswax";
    }
}
