#!/bin/bash
# P22 (D2): hash the stock helpers of the D2 libcoreclr.so and compare with the main+PR baseline.
# The baseline dump (/root/d1base/hash-mainpr) comes from P19 scripts/helperhash.py on the main+PR build.
. "$(dirname "$0")/env.sh"
B=${1:-$T/artifacts/bin/coreclr/linux.x64.Release}
OUT=${2:-$D/hash-d2}
rm -rf $OUT
python3 $W/docs/design/lxr-port/P19-integration/scripts/helperhash.py $B/libcoreclr.so $B/libcoreclr.so.dbg --dump $OUT 2>&1 | tail -40
echo "baseline TOTAL 46cee8bd87fd6252f5265267148d0a34"
diff -rq /root/d1base/hash-mainpr $OUT && echo "DUMPS IDENTICAL"
