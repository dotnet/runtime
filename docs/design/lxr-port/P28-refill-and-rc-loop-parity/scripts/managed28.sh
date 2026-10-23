#!/bin/bash
# P28: managed cells (P17 run-managed.py) with the Debug collector frame and ReadyToRun enabled, all three copy modes, with the
# fixtures of /root/p28/fixtures (mkfixtures28.sh). Same as /root/p25/scripts/run-managed.sh otherwise.
# usage: managed28.sh <frame> <out-dir>
FRAME=$1; OUT=$2
mkdir -p $OUT
SRC=/root/p25/main/docs/design/lxr-port/P17-pause-cost-and-reserve/run-managed.py
sed 's/for native in ("Release", "Debug"):/for native in os.environ.get("LXR_NATIVES", "Release,Debug").split(","):/' $SRC > $OUT.run-managed.py
LXR_NATIVES=${LXR_NATIVES:-Debug} python3 $OUT.run-managed.py --frames Debug=$FRAME,Release=$FRAME --modules Debug=$FRAME/libclrlxrgc.so,Release=$FRAME/libclrlxrgc.so \
  --fixtures /root/p28/fixtures --output $OUT > $OUT.txt 2>&1
echo "exit=$?" >> $OUT.txt
tail -4 $OUT.txt
echo "ok cells: $(grep -c ' ok$' $OUT.txt)"
grep -E 'FAILED' $OUT.txt | head
