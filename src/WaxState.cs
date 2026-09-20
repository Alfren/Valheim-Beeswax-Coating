using UnityEngine;

namespace BeeswaxCoating
{
    /// <summary>Per-piece waxed flag stored in the piece ZDO so it persists and syncs.</summary>
    internal static class WaxState
    {
        private const string WaxedKey = "waxed";

        public static bool IsWaxed(WearNTear wnt)
        {
            var nview = GetValidNetView(wnt);
            return nview != null && nview.GetZDO().GetBool(WaxedKey);
        }

        public static void SetWaxed(WearNTear wnt, bool value)
        {
            var nview = GetValidNetView(wnt);
            if (nview == null)
            {
                return;
            }
            nview.ClaimOwnership();
            nview.GetZDO().Set(WaxedKey, value);
        }

        private static ZNetView GetValidNetView(Component c)
        {
            if (c == null)
            {
                return null;
            }
            var nview = c.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return null;
            }
            return nview;
        }
    }
}
