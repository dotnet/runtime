#!/usr/bin/env bash
# Runs a repro file-based app on the shared framework built from this repo (./build.sh clr+libs),
# for findings that only reproduce on main. Usage: ./run-on-local-runtime.sh repros/01-SslStream-ZeroLengthRecord.cs
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
repo="$(cd "$here/../../../.." && pwd)"
testhost="$(ls -d "$repo"/artifacts/bin/testhost/net*-*-x64 2>/dev/null | head -n 1)"
[ -x "$testhost/dotnet" ] || { echo "No testhost found under $repo/artifacts/bin/testhost; build the runtime first (./build.sh clr+libs)." >&2; exit 1; }
out="$(mktemp -d)"
dotnet build "$1" -o "$out" > "$out/build.log" || { cat "$out/build.log"; exit 1; }
# The repro targets the SDK's framework version; roll forward onto the locally built one.
DOTNET_ROLL_FORWARD=Major "$testhost/dotnet" exec "$out/$(basename "${1%.cs}").dll"
