#!/bin/bash
# P28: the final benchmark in one session, one run at a time (nothing else runs). Method of P20/P23/P25/P26 (bench.sh of P25:
# 3 repetitions in rotated order, arms wks server lxr-nonmoving lxr-nursery, heap 4x, ReadyToRun on, natural collections, then
# one traced run per LXR cell for the pause medians). Frames: final = tip of the P28 branch, base = P26 final (start of P28).
# usage: bench-all28.sh <tag-prefix>        results: /root/p25/results/<prefix>-<step>-*
P=${1:-s28}
R=/root/p25/results
for step in f10e base; do
  /root/p25/scripts/bench.sh /root/p25/frames/p28-$step-Release-linux $P-$step > $R/$P-$step-bench.out 2>&1
  echo "done $step $(date +%T)"
done
echo done > $R/$P.done
