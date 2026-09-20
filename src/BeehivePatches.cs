using HarmonyLib;
using UnityEngine;

namespace BeeswaxCoating
{
    /// <summary>
    /// Vanilla beehives produce honey only. This patch gives them a second
    /// accumulator, beeswax, produced in lockstep with honey: every production
    /// tick that grants honey also grants wax (clamped to the hive's honey cap,
    /// and wax keeps accumulating while below its own cap even if honey is
    /// full). Harvesting extracts and drops both, mirroring the vanilla honey
    /// drop exactly. State lives in the hive ZDO, so it syncs and persists just
    /// like the vanilla honey level.
    /// </summary>
    [HarmonyPatch]
    internal static class BeehivePatches
    {
        private const string WaxLevelKey = "bwc_waxLevel";

        /// <summary>
        /// Production: IncreseLevel is called (owner-side only) whenever the hive
        /// grants honey units, including while honey sits at its cap - so no
        /// separate timer or condition bookkeeping is needed.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Beehive), "IncreseLevel")]
        private static void IncreseLevel_AccumulateWax(Beehive __instance, int i)
        {
            if (i <= 0 || !BeeswaxCoatingPlugin.HiveBeeswax.Value)
            {
                return;
            }
            ZNetView nview = __instance.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return;
            }
            ZDO zdo = nview.GetZDO();
            int wax = Mathf.Min(zdo.GetInt(WaxLevelKey) + i, __instance.m_maxHoney);
            zdo.Set(WaxLevelKey, wax);
        }

        /// <summary>
        /// Extraction: drops the accumulated wax with the same per-item offsets
        /// vanilla uses for honey, then resets the accumulator.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Beehive), "RPC_Extract")]
        private static void RPC_Extract_DropWax(Beehive __instance)
        {
            ZNetView nview = __instance.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid() || ObjectDB.instance == null)
            {
                return;
            }
            ZDO zdo = nview.GetZDO();
            int wax = zdo.GetInt(WaxLevelKey);
            if (wax <= 0)
            {
                return;
            }
            GameObject prefab = ObjectDB.instance.GetItemPrefab(CoatingItem.WaxPrefabName);
            if (prefab == null)
            {
                return;
            }
            for (int i = 0; i < wax; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 0.5f;
                Vector3 position = __instance.m_spawnPoint.position + new Vector3(offset.x, 0.25f * (i + 1), offset.y);
                Object.Instantiate(prefab, position, Quaternion.identity);
            }
            zdo.Set(WaxLevelKey, 0);
        }

        // Hover readout mirrors the vanilla "( Honey x N )" line.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Beehive), nameof(Beehive.GetHoverText))]
        private static void GetHoverText_ShowWax(Beehive __instance, ref string __result)
        {
            int wax = GetWaxLevel(__instance);
            if (wax > 0)
            {
                __result += "\n( Beeswax x " + wax + " )";
            }
        }

        private static int GetWaxLevel(Beehive hive)
        {
            ZNetView nview = hive.GetComponent<ZNetView>();
            return (nview != null && nview.IsValid()) ? nview.GetZDO().GetInt(WaxLevelKey) : 0;
        }
    }
}
