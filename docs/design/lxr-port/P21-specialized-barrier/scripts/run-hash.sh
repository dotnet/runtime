#!/bin/bash
# P21 (D1): hash the stock helpers of the D1 libcoreclr.so and compare with the main+PR baseline.
W=/mnt/c/Users/konradkokosa/.copilot/repos/copilot-worktrees/runtime-fork/kkokosa-expert-fortnight
B=${1:-/root/d1/main/artifacts/bin/coreclr/linux.x64.Release}
OUT=${2:-/root/d1/hash-d1}
rm -rf $OUT
python3 $W/docs/design/lxr-port/P19-integration/scripts/helperhash.py $B/libcoreclr.so $B/libcoreclr.so.dbg --dump $OUT 2>&1 | tail -40
echo "baseline TOTAL 46cee8bd87fd6252f5265267148d0a34"
diff -rq /root/d1base/hash-mainpr $OUT && echo "DUMPS IDENTICAL"
