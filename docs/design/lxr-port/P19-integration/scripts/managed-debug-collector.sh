#!/bin/bash
# Run the managed LXR cells against the Debug collector (63 process runs: 3 modes x (18 cases with the Release
# fixture + 3 cases with the Debug fixture)). The fixtures are the compiled managed tests of
# docs/design/lxr-port/P5/runtime-tests (fixture-scope5-Release and fixture-scope5-Debug).
# usage: managed-debug-collector.sh <p17-scripts-dir> <frames-root> <label> <fixtures-dir> <out-dir>
set -eu
P17=$1; FR=$2; L=$3; FIX=$4; OUT=$5
# P17's run-managed.py runs Release and Debug collectors. This copy keeps only the collectors in LXR_NATIVES.
sed 's/for native in ("Release", "Debug"):/for native in os.environ.get("LXR_NATIVES", "Release,Debug").split(","):/' "$P17/run-managed.py" > "$OUT.run-managed.py"
LXR_NATIVES=Debug python3 "$OUT.run-managed.py" \
  --frames Release="$FR/$L-Release-linux",Debug="$FR/$L-Debug-linux" \
  --modules Release="$FR/$L-Release-linux/libclrlxrgc.so",Debug="$FR/$L-Debug-linux/libclrlxrgc.so" \
  --fixtures "$FIX" --output "$OUT"
