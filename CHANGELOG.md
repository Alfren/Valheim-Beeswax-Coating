# Changelog

## 1.6.2
- Hardening pass: shader fallback can no longer throw on exotic builds (returns a material-less renderer instead), dedicated servers are now detected via the headless graphics device (the old ZNet-based check fired before ZNet existed, so servers built unused visual assets), and two stale contract tests plus outdated README claims were removed

## 1.6.1
- Review cleanup: removed a stale contract test and outdated hive-era strings in code comments, the legacy item description and the README

## 1.6.0
- Simplified acquisition: the Beeswax Coating is now crafted directly from 1x Honey + 2x Resin per brush (honey comes from vanilla beehives)
- Removed hive beeswax production; the Beeswax item remains registered as a legacy item so existing worlds and inventories stay valid, but nothing produces or consumes it

## 1.5.2
- Doubled the default brush durability to 20 charges per craft (config range widened to 1-100)

## 1.5.1
- Angled and roof-family wood pieces (26°/45° walls, beams, roof panels) can now be coated - eligibility is based on the piece's wood material instead of vanilla's no-roof-wear flag, which exempted them; stone and other materials are still refused
- The brush now equips instantly like the hammer (vanilla default is a 1-second equip animation)

## 1.5.0
- Beehives now accumulate beeswax exactly like honey: same production cadence, same cap, shown on hover as "( Beeswax x N )", and the full accumulated amount drops on harvest (replaces the flat per-harvest drop; `HiveBeeswaxPerHarvest` config replaced by the `HiveBeeswax` toggle)

## 1.4.1
- Fixed the brush playing an unarmed punch on left-click: it is now a one-handed weapon type (Tools are not returned by GetCurrentWeapon, which made clicks fall through to the bare-fist attack) and is held in one hand as intended
- Held-item detection now matches by drop prefab (immune to name tokenization), with the display name as fallback

## 1.4.0
- Beeswax Coating is now a multi-use brush: 10 charges per craft (configurable `BrushUses`, 1-50), shown as the native durability bar; the brush is only consumed when its charges run out
- Recipe rebalanced: 2x Beeswax + 1x Resin -> 1 brush (10 uses)
- Packaging: CHANGELOG.md shipped in the package; build now asserts manifest and project versions match

## 1.3.0
- Application reworked: hold the coating in the hotbar and left-click wood to apply it (gamepad attack works too); hold-and-sweep paints multiple pieces
- Removed the `ApplyKey` hotkey and `CoatingsPerUse` config (obsolete with the held-item mechanic)

## 1.2.1
- Coated wood now carries a subtle golden sheen visible without hovering (`CoatedSheen`, `SheenIntensity` config)
- Hover readout changed to a dedicated "Beeswax coated" line

## 1.2.0
- Fully custom procedural assets (honeycomb-chunk mesh, wax texture, generated icons) - no vanilla prefab reuse
- Items registered on dedicated servers via empty prefabs, fixing server-side registration

## 1.1.0
- Beehive harvests drop Beeswax (configurable `HiveBeeswaxPerHarvest`, 0-4)

## 1.0.0
- Initial release: rain/no-roof weathering immunity for coated wood, custom item + recipe, ZDO-persisted waxed state, server and client support
