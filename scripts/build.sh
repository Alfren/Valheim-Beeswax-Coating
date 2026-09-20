#!/usr/bin/env bash
# Compile the mod inside the docker toolchain.
set -euo pipefail
cd "$(dirname "$0")/.."

UID_GID="$(id -u):$(id -g)"

docker run --rm \
  --user "$UID_GID" \
  -e HOME=/tmp/home \
  -v "$PWD:/src" \
  beeswax-build 'dotnet build src -c Release --no-incremental'
