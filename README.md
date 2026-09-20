# BeeswaxCoating

A Valheim mod that lets you protect exposed wooden structures from rain and weathering with **Beeswax Coating**.

Exposed wood normally soaks up rain and slowly rots — rain wears wood down to 50% health (5% max-HP damage per 60s of wetness) and holds it there. With this mod you can craft a coating and brush it onto wood pieces — coated pieces are **permanently immune to rain and no-roof weathering**. Structural support failure, fire, ash, lava damage and full water submersion still apply. The coating is stored per piece in the world save, so it persists across sessions and works in multiplayer. Works on any weatherable wood piece, ships included.

## Crafting

**Beeswax Coating** — 3 per craft at the Workbench (level 1):

| Ingredient | Amount |
|---|---|
| Beeswax | 2 |
| Resin | 1 |

The item reuses the vanilla beeswax model and icon.

### Getting beeswax

Vanilla Valheim has **no beeswax item** — this mod adds one, with fully custom procedural assets: a jittered honeycomb-chunk mesh, a wax texture with a hex-cell pattern, and a hand-rasterized inventory icon, all generated in code (no vanilla prefab is cloned or reused). Player-built beehives accumulate beeswax **exactly like honey** — same production rate, same cap (4 by default), hover shows `( Beeswax x N )` — and harvesting drops the full accumulated amount. Disable with `HiveBeeswax = false`. The Beeswax Coating brush uses a paler, treated-wax variant of the same custom assets.

## Usage

1. Craft a Beeswax Coating brush at a workbench (2× Beeswax + 1× Resin → 1 brush with 10 charges).
2. Put it in your hotbar and equip it — it's held like a one-handed item, and the durability bar shows remaining charges.
3. Aim at a wood building piece and **left-click** to brush on the coating. Works with gamepad (attack trigger). Any wood-family material can be coated - walls, poles, beams, angled/roof wood included; stone and other materials are refused.
4. **Hold left-click and sweep** to wax several pieces in one pass; messages are throttled while sweeping.
5. The brush is consumed only when its charges run out. Coating does not repair existing damage; use the hammer for that.

## Configuration

`BepInEx/config/benjamin.beeswaxcoating.cfg`

| Option | Default | Description |
|---|---|---|
| `ShowHoverBadge` | `true` | Append a "Beeswax coated" line to hover text |
| `BrushUses` | `20` | Charges per crafted brush (1-100; applies to new brushes) |
| `HiveBeeswax` | `true` | Beehives accumulate beeswax like honey and drop it on harvest |
| `CoatedSheen` | `true` | Subtle warm sheen on coated wood, visible without hovering |
| `SheenIntensity` | `0.15` | Strength of the coated-wood sheen (0.05-0.5) |

## Recognizing coated wood

- **Sheen**: coated pieces carry a faint golden glow (the same material channel the game uses for structural highlighting), re-applied every second while a piece is loaded - visible from a distance, most noticeable at night or indoors.
- **Hover**: aiming at a piece shows a dedicated amber "Beeswax coated" line.

## Multiplayer

Install the mod on the server **and** all clients (enforced by the Jotunn version check — mismatched peers are refused rather than silently desyncing). The waxed flag is stored in each piece's ZDO and syncs automatically; wear is simulated by whichever peer owns the piece, which is why the server needs the mod too.

## Compatibility

Built against the current public build (game assemblies are pulled from the Valheim dedicated server at build time). No vanilla files are modified; everything is Harmony-patched at runtime.

## Installation

### Mod manager (recommended)
Install via r2modman, Gale or Thunderstore Mod Manager from Thunderstore.

### Manual
1. Install [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) and [Jotunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/).
2. Extract this mod so `BeeswaxCoating.dll` ends up in `Valheim/BepInEx/plugins/`.

## Building from source

A fully containerized toolchain is included (Docker required):

```sh
scripts/image.sh        # build the toolchain image (dotnet 8 + steamcmd + ilspycmd)
scripts/fetch-refs.sh   # download Valheim dedicated server via steamcmd -> refs/ (cached)
scripts/fetch-libs.sh   # download BepInEx + Jotunn from Thunderstore -> libs/
scripts/decompile.sh    # decompile game + Jotunn assemblies for reference
scripts/build.sh        # compile the mod
scripts/package.sh      # produce dist/BeeswaxCoating-<version>.zip
scripts/make-icon.py    # procedural fallback icon (used only if no logo is present)
```

Place a `BeeswaxLogoSmall.png` (or `BeeswaxLogo.png`) at the repo root to use your own icon — packaging converts it to the Thunderstore-required 256×256 (letterboxed; requires Pillow) and takes priority over the fallback.

## Testing

```sh
scripts/test.sh           # unit + contract tests (dotnet test, runs in the container)
scripts/check-package.sh  # validate the packaged zip (run package.sh first)
```

The test suite has three layers:

- **Pure logic** (`WaxDecisionTests`): the coating decision state machine is extracted into `src/WaxDecision.cs`, a Unity-free class that the tests compile directly — the shipped source is the tested source.
- **Game API contracts** (`GameContractTests`): loads the compiled DLL *and* the real game assemblies reflection-only via `MetadataLoadContext` (no Unity runtime needed) and asserts every member the mod depends on — `WearNTear.HaveRoof` (the immunity patch target), `Inventory.CountItems/RemoveItem`, the UI-guard statics, `ZDO` bool storage, and that the Harmony patch attributes point at members that still exist. **A failure here means a game update broke the mod — re-verify before shipping.**
- **Packaging** (`check-package.sh`): zip layout (manifest at root), manifest schema, pinned dependencies, 256×256 icon, DLL sanity.

What can't be automated without the game itself: Unity runtime behavior, Harmony patch application, and multiplayer sync — those remain the manual in-game smoke test.

## Dedicated server (Docker)

A fully modded dedicated server is included, mirroring the pinned mod list in `docker/server-mods.txt`:

```sh
scripts/fetch-server-mods.sh   # download + stage the server mods (cached)
scripts/server-build.sh        # build the valheim-beeswax image (uses the cached server files)
docker compose -f docker/compose.yaml up -d
docker logs -f valheim-beeswax
```

- **No password, LAN-only** (`-public 0`): connect from the game via *Join IP* → `<host>:2456`
- World persists in the `valheim-config` docker volume; UDP 2456–2457 exposed
- On dedicated servers the mod registers a visual-less item (server builds don't ship item visuals) with the same name token clients use — client-side you still see the cloned beeswax model
- Update the mod list by editing `docker/server-mods.txt` and re-running the two scripts

## License

MIT
