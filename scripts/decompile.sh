#!/usr/bin/env bash
# Decompile Assembly-CSharp (and Jotunn) for API verification.
set -euo pipefail
cd "$(dirname "$0")/.."

UID_GID="$(id -u):$(id -g)"

docker run --rm \
  --user "$UID_GID" \
  -e HOME=/tmp/home \
  -v "$PWD:/src" \
  beeswax-build '
    set -e
    mkdir -p decompiled decompiled-jotunn
    ilspycmd -p -o decompiled --nested-directories refs/assembly_valheim.dll
    ilspycmd -p -o decompiled-jotunn --nested-directories libs/Jotunn.dll
    echo OK
  '
