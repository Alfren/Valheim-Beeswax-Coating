#!/usr/bin/env bash
# Build the docker toolchain image.
set -euo pipefail
cd "$(dirname "$0")/.."
docker build -t beeswax-build docker/
