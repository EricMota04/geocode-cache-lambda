#!/usr/bin/env bash
# Builds the Lambda deployment zip that Terraform deploys.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="$ROOT/artifacts/GeocodeCache.Lambda.zip"

mkdir -p "$ROOT/artifacts"

dotnet lambda package \
  --project-location "$ROOT/src/GeocodeCache.Lambda" \
  --configuration Release \
  --output-package "$OUT"

echo "Packaged: $OUT"
