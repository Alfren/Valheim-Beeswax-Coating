#!/usr/bin/env bash
# Package the Thunderstore-ready zip into dist/.
set -euo pipefail
cd "$(dirname "$0")/.."

VERSION="$(grep -oPm1 '(?<=<Version>)[^<]+' src/BeeswaxCoating.csproj)"
JOTUNN_VER="$(cat libs/jotunn.version)"
OUT="dist"

DLL="src/bin/Release/BeeswaxCoating.dll"
if [ ! -f "$DLL" ]; then
  echo "ERROR: $DLL not found - run scripts/build.sh first" >&2
  exit 1
fi

# Icon: prefer the project logos at the repo root, else the committed icon,
# else generate a procedural one.
if [ -f BeeswaxLogoSmall.png ]; then
  python3 scripts/make-icon-from-logo.py BeeswaxLogoSmall.png packaging/icon.png
elif [ -f BeeswaxLogo.png ]; then
  python3 scripts/make-icon-from-logo.py BeeswaxLogo.png packaging/icon.png
elif [ ! -f packaging/icon.png ]; then
  python3 scripts/make-icon.py
fi

rm -rf "$OUT" && mkdir -p "$OUT/stage/BepInEx/plugins"
cp "$DLL" "$OUT/stage/BepInEx/plugins/"
cp README.md "$OUT/stage/"
cp CHANGELOG.md "$OUT/stage/"
cp packaging/icon.png "$OUT/stage/"
# Second copy inside BepInEx/plugins/: mod managers (Gale) only look for
# icon.png in the extracted install dir, not zip metadata - BepInEx ignores
# non-DLL files there, so this is harmless and makes the thumbnail show.
cp packaging/icon.png "$OUT/stage/BepInEx/plugins/"
sed "s/@JOTUNN@/$JOTUNN_VER/" packaging/manifest.json > "$OUT/stage/manifest.json"

# Thunderstore requires manifest.json at the zip root
(
  cd "$OUT/stage"
  zip -qr "../BeeswaxCoating-$VERSION.zip" .
)
rm -rf "$OUT/stage"

echo "Packaged dist/BeeswaxCoating-$VERSION.zip"
