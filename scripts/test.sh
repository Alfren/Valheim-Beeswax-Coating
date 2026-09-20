#!/usr/bin/env bash
# Run the unit + contract tests inside the docker toolchain.
set -euo pipefail
cd "$(dirname "$0")/.."

UID_GID="$(id -u):$(id -g)"

docker run --rm \
  --user "$UID_GID" \
  -e HOME=/tmp/home \
  -v "$PWD:/src" \
  beeswax-build 'dotnet test tests/BeeswaxCoating.Tests -c Release'
