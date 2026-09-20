#!/usr/bin/env bash
# Download the Valheim dedicated server (anonymous, free) via steamcmd and
# copy the managed game assemblies into refs/ for compiling against.
# The server install is cached in .cache/valheim-server so this only downloads once.
set -euo pipefail
cd "$(dirname "$0")/.."

UID_GID="$(id -u):$(id -g)"

docker run --rm \
  --user "$UID_GID" \
  -e HOME=/tmp/home \
  -v "$PWD:/src" \
  beeswax-build '
    set -e
    mkdir -p /src/.cache/valheim-server /src/refs
    /opt/steamcmd/steamcmd.sh \
      +force_install_dir /src/.cache/valheim-server \
      +login anonymous \
      +app_update 896660 validate \
      +quit
    cp /src/.cache/valheim-server/valheim_server_Data/Managed/assembly_valheim.dll /src/refs/
    cp /src/.cache/valheim-server/valheim_server_Data/Managed/assembly_utils.dll /src/refs/
    cp /src/.cache/valheim-server/valheim_server_Data/Managed/assembly_guiutils.dll /src/refs/
    cp /src/.cache/valheim-server/valheim_server_Data/Managed/netstandard.dll /src/refs/
    cp /src/.cache/valheim-server/valheim_server_Data/Managed/Assembly-CSharp*.dll /src/refs/ || true
    cp /src/.cache/valheim-server/valheim_server_Data/Managed/UnityEngine*.dll /src/refs/
    ls -la /src/refs/
  '
