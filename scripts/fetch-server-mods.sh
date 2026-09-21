#!/usr/bin/env bash
# Download the pinned server mods from Thunderstore and extract their BepInEx
# payloads into docker/server-build/server/. Also stages BeeswaxCoating.dll
# from the locally built package (dist/).
set -euo pipefail
cd "$(dirname "$0")/.."

MODS_FILE="docker/server-mods.txt"
DL_DIR=".cache/downloads/server-mods"
STAGE="docker/server-build/server/BepInEx"

mkdir -p "$DL_DIR" "$STAGE"

while IFS=- read -r owner name version; do
  case "${owner:-}" in ''|'#'*) continue ;; esac
  [ -z "${name:-}" ] && continue
  id="${owner}-${name}-${version}"
  zip="$DL_DIR/${id}.zip"
  if [ ! -f "$zip" ]; then
    echo "Downloading $id"
    curl -fL --retry 3 -o "$zip" "https://thunderstore.io/package/download/${owner}/${name}/${version}/"
  else
    echo "Cached $id"
  fi

  python3 - "$zip" "$STAGE" <<'PYEOF'
import sys, zipfile, os

src, dest = sys.argv[1], sys.argv[2]
META = {"manifest.json", "icon.png", "README.md", "CHANGELOG.md", "LICENSE"}
# top-level dirs mod managers map into BepInEx/
MAPPED = {"plugins", "patchers", "core", "monomod", "config"}

with zipfile.ZipFile(src) as z:
    for info in z.infolist():
        name = info.filename.replace("\\", "/")
        if name.endswith("/"):
            continue
        parts = name.split("/")
        if parts[0] == "BepInEx":
            parts = parts[1:]
        elif parts[0] in MAPPED:
            pass  # already relative to BepInEx
        elif parts[0] in META:
            continue  # zip metadata
        elif len(parts) == 1:
            # flat layout: file at zip root -> BepInEx/plugins/
            parts = ["plugins"] + parts
        else:
            continue  # anything else (loader binaries etc.) - not for a server
        if not parts or parts[-1] == "":
            continue
        out = os.path.join(dest, *parts)
        os.makedirs(os.path.dirname(out), exist_ok=True)
        with z.open(info) as f, open(out, "wb") as g:
            g.write(f.read())
PYEOF
done < "$MODS_FILE"

# Our mod from the locally built package
DIST_ZIP=$(ls dist/HoneySeal-*.zip | head -1)
python3 - "$DIST_ZIP" "$STAGE" <<'PYEOF'
import sys, zipfile, os
src, dest = sys.argv[1], sys.argv[2]
with zipfile.ZipFile(src) as z:
    for info in z.infolist():
        name = info.filename.replace("\\", "/")
        if name == "BepInEx/plugins/BeeswaxCoating.dll":
            out = os.path.join(dest, "plugins", "BeeswaxCoating.dll")
            with z.open(info) as f, open(out, "wb") as g:
                g.write(f.read())
            print("staged", out)
PYEOF

echo "--- staged BepInEx tree:"
find "$STAGE" -type f | sort
