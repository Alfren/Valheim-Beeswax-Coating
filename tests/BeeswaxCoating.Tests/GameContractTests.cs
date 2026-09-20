using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Sdk;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace BeeswaxCoating.Tests
{
    /// <summary>
    /// Loads the compiled mod DLL plus the real game assemblies reflection-only
    /// (MetadataLoadContext - no Unity runtime required) and asserts every
    /// game-API assumption the mod relies on. A failure here means a game update
    /// broke the mod's contract: re-check the decompiled sources before shipping.
    /// </summary>
    public class GameContractTests
    {
        private static readonly Lazy<Assemblies> Asms = new Lazy<Assemblies>(() => new Assemblies());

        private sealed class Assemblies : IDisposable
        {
            private readonly MetadataLoadContext _mlc;
            public Assembly Game { get; }
            public Assembly GuiUtils { get; }
            public Assembly Mod { get; }

            public Assemblies()
            {
                string root = FindRepoRoot();
                string managed = Path.Combine(root, ".cache", "valheim-server", "valheim_server_Data", "Managed");
                string libs = Path.Combine(root, "libs");
                string modDll = Path.Combine(root, "src", "bin", "Release", "BeeswaxCoating.dll");

                if (!Directory.Exists(managed))
                {
                    throw new XunitException($"Game assemblies not found at {managed} - run scripts/fetch-refs.sh first");
                }
                if (!File.Exists(modDll))
                {
                    throw new XunitException($"Mod DLL not found at {modDll} - run scripts/build.sh first");
                }

                var paths = Directory.EnumerateFiles(managed, "*.dll")
                    .Concat(Directory.EnumerateFiles(libs, "*.dll"))
                    .Append(modDll)
                    .ToList();

                _mlc = new MetadataLoadContext(new PathAssemblyResolver(paths), coreAssemblyName: "mscorlib");
                Game = _mlc.LoadFromAssemblyPath(Path.Combine(managed, "assembly_valheim.dll"));
                GuiUtils = _mlc.LoadFromAssemblyPath(Path.Combine(managed, "assembly_guiutils.dll"));
                Mod = _mlc.LoadFromAssemblyPath(modDll);
            }

            public void Dispose()
            {
                _mlc.Dispose();
            }

            public Assembly LoadFromPath(string path)
            {
                return _mlc.LoadFromAssemblyPath(path);
            }

            private static string FindRepoRoot()
            {
                DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null)
                {
                    if (Directory.Exists(Path.Combine(dir.FullName, "scripts")))
                    {
                        return dir.FullName;
                    }
                    dir = dir.Parent;
                }
                throw new XunitException("Could not locate repository root (no scripts/ directory found above " + AppContext.BaseDirectory + ")");
            }
        }

        private Assemblies A => Asms.Value;

        private Type GameType(string name)
        {
            Type? t = A.Game.GetType(name);
            Assert.True(t != null, $"assembly_valheim.dll no longer defines {name}");
            return t!;
        }

        private static void AssertMethod(Type type, string name, params int[] paramCounts)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                .Where(m => m.Name == name).ToList();
            Assert.True(methods.Count > 0, $"{type.Name}.{name}() no longer exists");
            Assert.Contains(methods, m => paramCounts.Contains(m.GetParameters().Length));
        }

        // ---- Game API assumptions used by WearPatches / WaxState ----

        [Fact]
        public void WearNTear_HasPrivateHaveRoof_UsedForWeatheringImmunity()
        {
            var wnt = GameType("WearNTear");
            var m = Assert.Single(wnt.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static),
                x => x.Name == "HaveRoof");
            Assert.True(m.DeclaringType == wnt, "HaveRoof moved to a base type - patch targeting may change");
        }

        [Fact]
        public void WearNTear_HasPublicNoRoofWearField()
        {
            var f = GameType("WearNTear").GetField("m_noRoofWear");
            Assert.True(f != null && f.IsPublic, "WearNTear.m_noRoofWear must stay a public field");
        }

        [Fact]
        public void HoverText_ExposeGetHoverText()
        {
            AssertMethod(GameType("HoverText"), "GetHoverText", 0);
        }

        // ---- Game API assumptions used by WaxInteraction ----

        [Fact]
        public void Player_ExposeLocalPlayer_HoverAndInventory()
        {
            var p = GameType("Player");
            Assert.True(p.GetField("m_localPlayer", BindingFlags.Public | BindingFlags.Static) != null,
                "Player.m_localPlayer must stay public static");
            AssertMethod(p, "GetHoverObject", 0);
            AssertMethod(p, "GetInventory", 0);
            AssertMethod(p, "Message", 2, 5);
        }

        [Fact]
        public void Inventory_CountAndRemoveByName()
        {
            var inv = GameType("Inventory");
            AssertMethod(inv, "CountItems", 1, 3);
            AssertMethod(inv, "RemoveItem", 2, 4);
        }

        [Fact]
        public void ZDO_BoolGetSet()
        {
            var zdo = GameType("ZDO");
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            var boolSet = zdo.GetMethods(flags).Where(m => m.Name == "Set" && m.GetParameters().Length == 2)
                .Where(m => m.GetParameters()[0].ParameterType.Name == "String" && m.GetParameters()[1].ParameterType.Name == "Boolean");
            Assert.True(boolSet.Any(), "ZDO.Set(string, bool) overload missing - waxed flag could not be stored");

            AssertMethod(zdo, "GetBool", 1, 2);
            var getBool = zdo.GetMethods(flags).Where(m => m.Name == "GetBool")
                .Where(m => m.ReturnType.Name == "Boolean");
            Assert.True(getBool.Any(), "ZDO.GetBool no longer returns bool");
        }

        [Fact]
        public void Piece_ExposeNameFieldForMessages()
        {
            var f = GameType("Piece").GetField("m_name");
            Assert.True(f != null && f.IsPublic && f.FieldType.Name == "String",
                "Piece.m_name must stay a public string field");
        }

        [Fact]
        public void ZNetView_OwnershipAndZdo()
        {
            var nv = GameType("ZNetView");
            AssertMethod(nv, "ClaimOwnership", 0);
            AssertMethod(nv, "GetZDO", 0);
            AssertMethod(nv, "IsValid", 0);
        }

        [Fact]
        public void UI_GuardsExist_ForHotkeySuppression()
        {
            AssertMethod(GameType("Menu"), "IsVisible", 0);
            AssertMethod(GameType("InventoryGui"), "IsVisible", 0);
            AssertMethod(GameType("Console"), "IsVisible", 0);
            AssertMethod(GameType("TextInput"), "IsVisible", 0);
            AssertMethod(GameType("Minimap"), "InTextInput", 0);
            AssertMethod(GameType("Hud"), "IsPieceSelectionVisible", 0);
        }

        [Fact]
        public void Localization_ExistsInGuiUtils()
        {
            Type? t = A.GuiUtils.GetType("Localization");
            Assert.True(t != null, "assembly_guiutils.dll no longer defines Localization");
            Assert.True(t.GetMember("instance").Any(), "Localization.instance missing");
            AssertMethod(t, "Localize", 1);
        }

        [Fact]
        public void Beehive_YieldsBeeswax_PatchTargetsExist()
        {
            var hive = GameType("Beehive");
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            Assert.Single(hive.GetMethods(flags), m => m.Name == "RPC_Extract" && m.GetParameters().Length == 1);
            Assert.Single(hive.GetMethods(flags), m => m.Name == "IncreseLevel" && m.GetParameters().Length == 1);
            AssertMethod(hive, "GetHoverText", 0);
            var maxHoney = hive.GetField("m_maxHoney");
            Assert.True(maxHoney != null && maxHoney.IsPublic, "Beehive.m_maxHoney must stay a public field");
            Assert.True(hive.GetField("m_spawnPoint") != null, "Beehive.m_spawnPoint field missing");
        }

        [Fact]
        public void ZNet_And_ObjectDB_Lookups_Exist()
        {
            Assert.True(GameType("ZNet").GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "GetUID" && m.GetParameters().Length == 0),
                "ZNet.GetUID() static missing - hive drop guard would break");
            AssertMethod(GameType("ObjectDB"), "GetItemPrefab", 1);
        }

        [Fact]
        public void Sheen_PatchTargets_Exist()
        {
            // WearNTear.UpdateWear drives the periodic sheen re-application
            AssertMethod(GameType("WearNTear"), "UpdateWear", 1);
            AssertMethod(GameType("MaterialMan"), "SetValue", 4);

            // ShaderProps lives in assembly_utils
            string utilsPath = System.IO.Path.Combine(FindManagedDir(), "assembly_utils.dll");
            var utils = Asms.Value.LoadFromPath(utilsPath);
            var shaderProps = utils.GetType("ShaderProps");
            Assert.True(shaderProps != null, "assembly_utils.dll no longer defines ShaderProps");
            Assert.True(shaderProps!.GetField("_EmissionColor") != null, "ShaderProps._EmissionColor missing");
        }

        [Fact]
        public void HotbarTool_UsePath_Exists()
        {
            var humanoid = GameType("Humanoid");
            AssertMethod(humanoid, "StartAttack", 2);
            AssertMethod(humanoid, "GetCurrentWeapon", 0);
            AssertMethod(humanoid, "UnequipItem", 1, 2);
            AssertMethod(GameType("Inventory"), "RemoveOneItem", 1);
            // Invoked via reflection after in-place durability changes
            AssertMethod(GameType("Inventory"), "Changed", 0, 2);

            var itemType = GameType("ItemDrop").GetNestedType("ItemData")?.GetNestedType("ItemType");
            Assert.True(itemType != null, "ItemDrop.ItemData.ItemType enum missing");
            Assert.True(Enum.GetNames(itemType!).Contains("OneHandedWeapon"), "ItemType.OneHandedWeapon missing");

            var itemData = GameType("ItemDrop").GetNestedType("ItemData");
            Assert.True(itemData?.GetField("m_dropPrefab") != null, "ItemData.m_dropPrefab missing");

            var shared = GameType("ItemDrop").GetNestedType("ItemData")?.GetNestedType("SharedData");
            Assert.True(shared?.GetField("m_maxDurability") != null, "SharedData.m_maxDurability missing");
            Assert.True(shared?.GetField("m_useDurability") != null, "SharedData.m_useDurability missing");
            Assert.True(shared?.GetField("m_equipDuration") != null, "SharedData.m_equipDuration missing");

            // Coating eligibility gates on the wood material family
            var materialType = GameType("WearNTear").GetNestedType("MaterialType");
            Assert.True(materialType != null, "WearNTear.MaterialType enum missing");
            var names = Enum.GetNames(materialType!);
            Assert.True(new[] { "Wood", "HardWood", "Timberwood" }.All(n => names.Contains(n)),
                "Wood material types missing from WearNTear.MaterialType");
            Assert.True(GameType("WearNTear").GetField("m_materialType")?.IsPublic == true,
                "WearNTear.m_materialType must stay public");
        }

        private static string FindManagedDir()
        {
            string root = FindRepoRootImpl();
            return System.IO.Path.Combine(root, ".cache", "valheim-server", "valheim_server_Data", "Managed");
        }

        private static string FindRepoRootImpl()
        {
            System.IO.DirectoryInfo? dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, "scripts")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new XunitException("Could not locate repository root");
        }

        // ---- Mod assembly sanity ----

        [Fact]
        public void Plugin_HasCorrectMetadata()
        {
            var plugin = A.Mod.GetType("BeeswaxCoating.BeeswaxCoatingPlugin");
            Assert.True(plugin != null, "Plugin type missing from compiled DLL");

            var attr = plugin!.GetCustomAttributesData()
                .FirstOrDefault(a => a.AttributeType.Name.EndsWith("BepInPlugin"));
            Assert.True(attr != null, "BepInPlugin attribute missing");
            Assert.Equal("benjamin.beeswaxcoating", (string)attr!.ConstructorArguments[0].Value!);
            Assert.Equal("BeeswaxCoating", (string)attr.ConstructorArguments[1].Value!);

            Assert.Contains(plugin.GetCustomAttributesData(),
                a => a.AttributeType.FullName == "Jotunn.Utils.NetworkCompatibilityAttribute");

            Assert.Contains(plugin.GetConstructors(), c => c.GetParameters().Length == 0 && c.IsPublic);
        }

        [Fact]
        public void HarmonyPatchTargets_ExistInGameAssembly()
        {
            var patches = A.Mod.GetType("BeeswaxCoating.WearPatches");
            Assert.True(patches != null, "WearPatches type missing from compiled DLL");

            var targets = new List<(string? DeclaringType, string? MethodName)>();
            foreach (var m in patches!.GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
            {
                foreach (var a in m.GetCustomAttributesData()
                    .Where(a => a.AttributeType.Name.EndsWith("HarmonyPatch")))
                {
                    string? type = null;
                    string? method = null;
                    var args = a.ConstructorArguments;
                    if (args.Count >= 1 && args[0].Value is Type t)
                    {
                        type = t.FullName;
                    }
                    if (args.Count >= 2 && args[1].Value is string s)
                    {
                        method = s;
                    }
                    targets.Add((type, method));
                }
            }

            Assert.Contains(targets, t => t.DeclaringType == "WearNTear" && t.MethodName == "HaveRoof");
            Assert.Contains(targets, t => t.DeclaringType == "HoverText" && t.MethodName == "GetHoverText");

            // Cross-check against the real game assembly
            Assert.Single(GameType("WearNTear").GetMethods(BindingFlags.NonPublic | BindingFlags.Instance), m => m.Name == "HaveRoof");
            Assert.Single(GameType("HoverText").GetMethods(), m => m.Name == "GetHoverText");
        }
    }
}
