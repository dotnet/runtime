#!/bin/bash
# P25: managed cells (P17 run-managed.py) with the Debug collector frame and ReadyToRun enabled, all three copy modes.
# usage: run-managed.sh <frame> <out-dir>     (the fixtures are the D2 fixtures in /root/d2/fixtures)
FRAME=$1; OUT=$2
mkdir -p $OUT
SRC=/root/p25/main/docs/design/lxr-port/P17-pause-cost-and-reserve/run-managed.py
sed 's/for native in ("Release", "Debug"):/for native in os.environ.get("LXR_NATIVES", "Release,Debug").split(","):/' $SRC > $OUT.run-managed.py
LXR_NATIVES=Debug python3 $OUT.run-managed.py --frames Debug=$FRAME --modules Debug=$FRAME/libclrlxrgc.so \
  --fixtures /root/d2/fixtures --output $OUT > $OUT.txt 2>&1
echo "exit=$?" >> $OUT.txt
tail -4 $OUT.txt
echo "ok cells: $(grep -c ' ok$' $OUT.txt)"
grep -E 'FAILED' $OUT.txt | head
