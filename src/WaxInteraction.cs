using BeeswaxCoating;
using UnityEngine;

namespace BeeswaxCoating
{
    internal static class WaxInteraction
    {
        internal static string CoatingSharedName;

        private static int _lastMessagePiece;
        private static WaxOutcome _lastMessageOutcome;
        private static float _lastMessageTime;

        public static void TryApply()
        {
            Player player = Player.m_localPlayer;
            if (player == null || ZNetScene.instance == null)
            {
                return;
            }
            if (Menu.IsVisible() || InventoryGui.IsVisible() || Console.IsVisible()
                || TextInput.IsVisible() || Minimap.InTextInput() || Hud.IsPieceSelectionVisible())
            {
                return;
            }

            // Must be holding the coating as the active hotbar item.
            ItemDrop.ItemData held = player.GetCurrentWeapon();
            if (held == null || string.IsNullOrEmpty(CoatingSharedName)
                || held.m_shared.m_name != CoatingSharedName)
            {
                return;
            }

            GameObject hover = player.GetHoverObject();
            Piece piece = hover == null ? null : hover.GetComponentInParent<Piece>();
            WearNTear wnt = piece == null ? null : piece.GetComponent<WearNTear>();

            WaxOutcome outcome = WaxDecision.Evaluate(
                piece != null,
                wnt != null,
                wnt != null && IsWood(wnt),
                wnt != null && WaxState.IsWaxed(wnt),
                1,
                1);

            switch (outcome)
            {
                case WaxOutcome.NotBuildingPiece:
                    Show(player, piece, outcome, "Only building pieces can be waxed");
                    break;
                case WaxOutcome.DoesNotWeather:
                    Show(player, piece, outcome, PieceName(piece) + " does not weather in the rain");
                    break;
                case WaxOutcome.AlreadyWaxed:
                    Show(player, piece, outcome, PieceName(piece) + " is already waxed");
                    break;
                case WaxOutcome.Applied:
                    Inventory inventory = player.GetInventory();
                    WaxState.SetWaxed(wnt, true);
                    WaxSheen.Apply(wnt);
                    held.m_durability -= 1f;
                    if (held.m_durability <= 0f)
                    {
                        player.UnequipItem(held);
                        inventory.RemoveOneItem(held);
                        player.Message(MessageHud.MessageType.TopLeft, "Beeswax coating applied - brush worn out");
                    }
                    else
                    {
                        MarkInventoryChanged(inventory);
                        player.Message(MessageHud.MessageType.TopLeft,
                            "Beeswax coating applied (" + Mathf.CeilToInt(held.m_durability) + " uses left)");
                    }
                    break;
            }
        }

        /// <summary>
        /// Hold-and-sweep ("paint mode") fires this every physics frame while the
        /// attack button is held, so identical messages for the same piece are
        /// throttled to one per second.
        /// </summary>
        private static void Show(Player player, Piece piece, WaxOutcome outcome, string text)
        {
            int id = piece != null ? piece.GetInstanceID() : 0;
            if (id == _lastMessagePiece && outcome == _lastMessageOutcome
                && Time.time - _lastMessageTime < 1f)
            {
                return;
            }
            _lastMessagePiece = id;
            _lastMessageOutcome = outcome;
            _lastMessageTime = Time.time;
            player.Message(MessageHud.MessageType.TopLeft, text);
        }

        private static readonly System.Reflection.MethodInfo InventoryChangedMethod =
            typeof(Inventory).GetMethod("Changed",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        /// <summary>Refresh inventory UI after an in-place durability change.</summary>
        private static void MarkInventoryChanged(Inventory inventory)
        {
            try
            {
                InventoryChangedMethod?.Invoke(inventory, new object[] { false, false });
            }
            catch (System.Exception e)
            {
                BeeswaxCoatingPlugin.Logger.LogDebug($"Inventory.Changed invoke failed: {e.Message}");
            }
        }

        /// <summary>
        /// Eligibility is material-based: any wood-family piece can be waxed,
        /// including angled/roof wood that vanilla exempts from weathering
        /// (m_noRoofWear == false). Stone and other materials are refused.
        /// </summary>
        private static bool IsWood(WearNTear wnt)
        {
            WearNTear.MaterialType t = wnt.m_materialType;
            return t == WearNTear.MaterialType.Wood
                || t == WearNTear.MaterialType.HardWood
                || t == WearNTear.MaterialType.Timberwood;
        }

        private static string PieceName(Piece piece)
        {
            return Localization.instance.Localize(piece.m_name);
        }
    }
}
