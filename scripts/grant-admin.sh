#!/usr/bin/env bash
# Grant a player admin on the modded server (by SteamID64) and restart it.
# Usage: scripts/grant-admin.sh <steamid64> [steamid64 ...]
set -euo pipefail
cd "$(dirname "$0")/.."

if [ $# -eq 0 ]; then
  echo "Usage: $0 <steamid64> [steamid64 ...]" >&2
  exit 1
fi

for id in "$@"; do
  if grep -qx "$id" <(docker exec valheim-beeswax cat /config/adminlist.txt 2>/dev/null || true); then
    echo "$id already admin"
  else
    docker exec valheim-beeswax sh -c "echo '$id' >> /config/adminlist.txt"
    echo "added $id to adminlist"
  fi
done

# adminlist is only read at startup
docker compose -f docker/compose.yaml restart
echo "server restarting - admin list reloads on boot"
