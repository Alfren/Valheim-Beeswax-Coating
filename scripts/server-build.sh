#!/usr/bin/env bash
# Assemble the docker build context and build the modded Valheim server image.
set -euo pipefail
cd "$(dirname "$0")/.."

CTX="docker/server-build"

# 1. Server files from the steamcmd cache (skip steamcmd bookkeeping + Windows bits)
rm -rf "$CTX"
mkdir -p "$CTX/server"
rsync -a --delete \
  --exclude 'steamapps' \
  --exclude 'docker' \
  --exclude 'docker_start_server.sh' \
  .cache/valheim-server/ "$CTX/server/"

# 2. BepInEx loader overlay (Linux parts of the BepInExPack)
PACK=".cache/downloads/x-bepinex/BepInExPack_Valheim"
rsync -a "$PACK/BepInEx/" "$CTX/server/BepInEx/"
rsync -a "$PACK/doorstop_libs/" "$CTX/server/doorstop_libs/"
cp "$PACK/doorstop_config.ini" "$CTX/server/"

# 3. Mods (downloaded + extracted by fetch-server-mods.sh)
if [ ! -d "$CTX/server/BepInEx/plugins" ]; then
  ./scripts/fetch-server-mods.sh
fi

# 4. Entrypoint
cp docker/server-entrypoint.sh "$CTX/server/entrypoint.sh"
chmod +x "$CTX/server/entrypoint.sh"

docker build -t valheim-beeswax -f docker/server.Dockerfile "$CTX"
echo "Image built: valheim-beeswax"
