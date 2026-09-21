using BeeswaxCoating;
using HarmonyLib;
using UnityEngine;

namespace BeeswaxCoating
{
    /// <summary>
    /// Subtle warm emission sheen on coated wood, visible without hovering.
    /// Uses the same MaterialMan property system as the game's structural
    /// highlighting. Re-applied from the UpdateWear postfix (~1s cadence per
    /// loaded piece, on every peer), so it self-heals when zones stream in,
    /// new players connect, or the hammer highlight resets materials.
    /// </summary>
    internal static class WaxSheen
    {
        private static readonly Color Tint = new Color(1f, 0.72f, 0.25f, 1f);

        public static void Apply(WearNTear wnt)
        {
            if (!BeeswaxCoatingPlugin.CoatedSheen.Value || MaterialMan.instance == null || wnt == null)
            {
                return;
            }
            float k = BeeswaxCoatingPlugin.SheenIntensity.Value;
            MaterialMan.instance.SetValue(wnt.gameObject, ShaderProps._EmissionColor, Tint * k);
        }
    }

    [HarmonyPatch]
    internal static class WearPatches
    {
        // HaveRoof() is private in the current build; patch by name. Returning true for waxed
        // pieces stops rain/no-roof weathering (m_rainWet / IsWet path in UpdateWear) and snow
        // buildup, while leaving support, fire, ash and lava wear untouched.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WearNTear), "HaveRoof")]
        private static void HaveRoof_Postfix(WearNTear __instance, ref bool __result)
        {
            if (!__result && WaxState.IsWaxed(__instance))
            {
                __result = true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.UpdateWear))]
        private static void UpdateWear_ApplySheen(WearNTear __instance)
        {
            if (WaxState.IsWaxed(__instance))
            {
                WaxSheen.Apply(__instance);
            }
        }

        // Building pieces expose hover text through the generic HoverText component.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HoverText), nameof(HoverText.GetHoverText))]
        private static void HoverText_GetHoverText_Postfix(HoverText __instance, ref string __result)
        {
            if (!BeeswaxCoatingPlugin.ShowHoverBadge.Value)
            {
                return;
            }
            var wnt = __instance.GetComponentInParent<WearNTear>();
            if (wnt != null && WaxState.IsWaxed(wnt))
            {
                __result += "\n<color=#F2B632>Honey sealed</color>";
            }
        }

        // The coating is a hotbar item: left-click (the Attack action, keyboard or
        // gamepad) with it held applies it to the aimed piece. Vanilla StartAttack
        // would no-op for an item without attack data, so the attack is swallowed.
        // Holding the button sweeps across pieces ("paint mode").
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
        private static bool StartAttack_UseCoating(Humanoid __instance, bool secondaryAttack)
        {
            if (__instance != Player.m_localPlayer || secondaryAttack)
            {
                return true;
            }
            if (!IsHeldCoating(__instance.GetCurrentWeapon()))
            {
                return true;
            }
            WaxInteraction.TryApply();
            return false;
        }

        private static bool IsHeldCoating(ItemDrop.ItemData held)
        {
            if (held == null || string.IsNullOrEmpty(WaxInteraction.CoatingSharedName))
            {
                return false;
            }
            // Match by drop prefab first (immune to any name tokenization), name as fallback.
            if (held.m_dropPrefab != null && held.m_dropPrefab.name == CoatingItem.PrefabName)
            {
                return true;
            }
            return held.m_shared.m_name == WaxInteraction.CoatingSharedName;
        }
    }
}
