#!/usr/bin/env bash
# Download BepInExPack Valheim + latest Jotunn from Thunderstore into libs/.
set -euo pipefail
cd "$(dirname "$0")/.."

BEPINEX_VER="5.4.2350"
JOTUNN_TEAM="ValheimModding"
mkdir -p .cache/downloads libs

dl() { # dl <url> <out>
  echo "Downloading $1"
  curl -fL --retry 3 -o "$2" "$1"
}

# Jotunn latest version from its package page
JOTUNN_VER=$(curl -fsSL "https://thunderstore.io/c/valheim/p/${JOTUNN_TEAM}/Jotunn/" \
  | grep -oE "download/${JOTUNN_TEAM}/Jotunn/[0-9]+\.[0-9]+\.[0-9]+/" | head -1 \
  | grep -oE '[0-9]+\.[0-9]+\.[0-9]+')
if [ -z "$JOTUNN_VER" ]; then
  echo "Could not determine Jotunn version" >&2
  exit 1
fi
echo "Jotunn latest: $JOTUNN_VER"
echo -n "$JOTUNN_VER" > libs/jotunn.version

dl "https://thunderstore.io/package/download/denikson/BepInExPack_Valheim/${BEPINEX_VER}/" ".cache/downloads/bepinex.zip"
dl "https://thunderstore.io/package/download/${JOTUNN_TEAM}/Jotunn/${JOTUNN_VER}/" ".cache/downloads/jotunn.zip"

rm -rf .cache/downloads/x-bepinex .cache/downloads/x-jotunn
mkdir -p .cache/downloads/x-bepinex .cache/downloads/x-jotunn
unzip -q .cache/downloads/bepinex.zip -d .cache/downloads/x-bepinex

# Jotunn zips use backslash path separators; unzip mangles them, so use python
python3 - <<'PYEOF'
import zipfile, os
src = '.cache/downloads/jotunn.zip'
dest = '.cache/downloads/x-jotunn'
with zipfile.ZipFile(src) as z:
    for info in z.infolist():
        name = info.filename.replace('\\', '/')
        if name.endswith('/') or not name or not name.endswith('.dll'):
            continue
        out = os.path.join(dest, *name.split('/'))
        os.makedirs(os.path.dirname(out), exist_ok=True)
        with z.open(info) as f, open(out, 'wb') as g:
            g.write(f.read())
        print('extracted', name)
PYEOF

cp .cache/downloads/x-bepinex/BepInExPack_Valheim/BepInEx/core/BepInEx.dll libs/
cp .cache/downloads/x-bepinex/BepInExPack_Valheim/BepInEx/core/0Harmony.dll libs/

find .cache/downloads/x-jotunn -name 'Jotunn*.dll' -exec cp {} libs/ \;

ls -la libs/
