#!/bin/bash
# P22 (D2): managed cells (P17 run-managed.py) with the Debug collector and ReadyToRun enabled.
# usage: run-managed.sh <frame> <out-dir> [natives: Debug|Release]
. "$(dirname "$0")/env.sh"
FRAME=$1; OUT=$2; NAT=${3:-Debug}
mkdir -p $OUT
sed 's/for native in ("Release", "Debug"):/for native in os.environ.get("LXR_NATIVES", "Release,Debug").split(","):/' \
  $W/docs/design/lxr-port/P17-pause-cost-and-reserve/run-managed.py > $OUT.run-managed.py
LXR_NATIVES=$NAT python3 $OUT.run-managed.py --frames $NAT=$FRAME --modules $NAT=$FRAME/libclrlxrgc.so \
  --fixtures $D/fixtures --output $OUT > $OUT.txt 2>&1
echo "exit=$?" >> $OUT.txt
tail -4 $OUT.txt
grep -c ' ok$' $OUT.txt
grep -E 'FAILED' $OUT.txt | head
