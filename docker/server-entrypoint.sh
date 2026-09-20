#!/bin/sh
# Entrypoint for the modded Valheim dedicated server container.
set -e
cd /valheim

# BepInEx bootstrap (mirrors the BepInExPack Valheim start_server_bepinex.sh)
export DOORSTOP_ENABLED=1
export DOORSTOP_TARGET_ASSEMBLY=./BepInEx/core/BepInEx.Preloader.dll
export LD_LIBRARY_PATH="./doorstop_libs:./linux64:${LD_LIBRARY_PATH}"
export LD_PRELOAD="libdoorstop_x64.so"
export SteamAppId=892970

# Writable HOME for steamclient.so logs
export HOME=/config/steamhome
mkdir -p "$HOME" /config

exec ./valheim_server.x86_64 \
  -name "${SERVER_NAME:-Beeswax Coating}" \
  -port "${SERVER_PORT:-2456}" \
  -world "${SERVER_WORLD:-Beeswax}" \
  -public 0 \
  -savedir /config \
  "$@"
