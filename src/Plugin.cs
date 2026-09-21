using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace BeeswaxCoating
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class BeeswaxCoatingPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "benjamin.beeswaxcoating";
        public const string PluginName = "HoneySeal";
        public const string PluginVersion = "2.0.1";

        internal new static ManualLogSource Logger;
        internal static ConfigEntry<bool> ShowHoverBadge;
        internal static ConfigEntry<int> BrushUses;
        internal static ConfigEntry<bool> CoatedSheen;
        internal static ConfigEntry<float> SheenIntensity;

        private Harmony _harmony;

        private void Awake()
        {
            Logger = base.Logger;

            ShowHoverBadge = Config.Bind(
                "General", "ShowHoverBadge", true,
                "Append a \"Honey sealed\" line to hover text.");

            BrushUses = Config.Bind(
                "General", "BrushUses", 20,
                new ConfigDescription("Charges per crafted Honey Seal brush (shown as the durability bar). Applies to newly crafted brushes.",
                    new AcceptableValueRange<int>(1, 100)));

            CoatedSheen = Config.Bind(
                "Visuals", "CoatedSheen", true,
                "Give coated wood a subtle warm sheen so protected pieces can be recognized without hovering.");

            SheenIntensity = Config.Bind(
                "Visuals", "SheenIntensity", 0.15f,
                new ConfigDescription("Strength of the coated-wood sheen.",
                    new AcceptableValueRange<float>(0.05f, 0.5f)));

            PrefabManager.OnVanillaPrefabsAvailable += AddCoatingItem;

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(BeeswaxCoatingPlugin).Assembly);

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private bool _itemRegistered;

        private void AddCoatingItem()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddCoatingItem;
            if (_itemRegistered)
            {
                return;
            }
            try
            {
                // Raw beeswax: legacy item, kept registered so items in existing
                // worlds/inventories stay valid. Nothing produces or consumes it -
                // the coating is crafted directly from honey and resin.
                RegisterItem(
                    CoatingItem.WaxPrefabName,
                    "Beeswax",
                    "A lump of amber beeswax. A legacy item - no longer used for crafting.",
                    null,
                    new Color(0.83f, 0.58f, 0.16f),
                    new Color(0.55f, 0.36f, 0.08f));

                // The coating: a multi-use hotbar item, applied by left-clicking
                // while holding it. Crafted at the workbench from honey and resin.
                // (Internal identifiers keep the historic beeswax naming so
                // existing worlds, inventories and server installs stay valid.)
                RegisterItem(
                    CoatingItem.PrefabName,
                    "Honey Seal",
                    "A soft lump of treated wax on a handle. Hold it and left-click exposed wood to brush on the coating.",
                    new ItemConfig
                    {
                        CraftingStation = "piece_workbench",
                        MinStationLevel = 1,
                        Amount = 1,
                        Requirements = new[]
                        {
                            new RequirementConfig("Honey", 1),
                            new RequirementConfig("Resin", 2)
                        }
                    },
                    new Color(0.94f, 0.87f, 0.64f),
                    new Color(0.72f, 0.62f, 0.34f),
                    isTool: true);

                _itemRegistered = true;
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to register items: {e}");
            }
        }

        private static void RegisterItem(string prefabName, string displayName, string description,
            ItemConfig recipeConfig, Color waxColor, Color cellColor, bool isTool = false)
        {
            var config = recipeConfig ?? new ItemConfig();
            config.Name = displayName;
            config.Description = description;

            // Empty prefab (cube primitive + ZNetView), then fully custom procedural
            // visuals - no vanilla prefab is cloned or reused.
            CustomItem item = new CustomItem(prefabName, addZNetView: true, config);
            GameObject go = item.ItemPrefab;

            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            // Held like a one-handed weapon: Humanoid.GetCurrentWeapon() only returns
            // weapon-type items (a Tool would fall through to the unarmed weapon and
            // left-clicks would punch instead). Empty attack animations keep the
            // vanilla attack path structurally disabled.
            shared.m_itemType = isTool
                ? ItemDrop.ItemData.ItemType.OneHandedWeapon
                : ItemDrop.ItemData.ItemType.Material;
            shared.m_maxStackSize = isTool ? 1 : 50;
            shared.m_weight = 0.1f;
            if (isTool)
            {
                // Charges per brush, surfaced as the native durability bar.
                shared.m_useDurability = true;
                shared.m_maxDurability = BrushUses.Value;
                item.ItemDrop.m_itemData.m_durability = BrushUses.Value;
                // Equip instantly like the hammer (SharedData default is 1s).
                shared.m_equipDuration = 0f;
                // The weapon item type is required for held-item detection, but a
                // fresh SharedData carries combat defaults (block 10, parry 1.5x,
                // backstab 4x, knockback 30, Swords skill) that the tooltip would
                // display on a paint brush - zero them so those lines are hidden.
                shared.m_skillType = Skills.SkillType.None;
                shared.m_blockPower = 0f;
                shared.m_timedBlockBonus = 0f;
                shared.m_attackForce = 0f;
                shared.m_backstabBonus = 0f;
            }
            else
            {
                shared.m_useDurability = false;
            }
            shared.m_icons = new[] { WaxAssets.CreateWaxIcon(waxColor, cellColor) };

            // Headless check: ZNet.instance is not yet assigned when item
            // registration runs (ObjectDB.CopyOtherDB during scene load), so
            // the reliable signal for a dedicated server is the missing
            // graphics device - no point building meshes/materials there.
            bool headless = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (!headless)
            {
                int seed = prefabName == CoatingItem.PrefabName ? 42 : 7;
                WaxAssets.AttachVisuals(go, waxColor, cellColor, seed);
            }

            // Dropped items live on the "item" layer and need a body for auto-pickup
            // (Player.AutoPickup matches colliders with an attached rigidbody).
            go.layer = LayerMask.NameToLayer("item");
            Rigidbody body = go.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = go.AddComponent<Rigidbody>();
                body.mass = 1f;
                body.angularDrag = 0.6f;
            }
            BoxCollider box = go.GetComponent<BoxCollider>();
            if (box != null)
            {
                box.size = new Vector3(0.45f, 0.3f, 0.45f);
                box.center = new Vector3(0f, 0.15f, 0f);
            }

            ItemManager.Instance.AddItem(item);
            if (prefabName == CoatingItem.PrefabName)
            {
                WaxInteraction.CoatingSharedName = shared.m_name;
            }
            Logger.LogInfo($"Registered {prefabName} (token: {shared.m_name}, custom assets)");
        }

        private void OnDestroy()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddCoatingItem;
            _harmony?.UnpatchSelf();
        }
    }
}
