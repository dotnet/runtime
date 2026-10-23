#!/bin/bash
# P26: the benchmark of every step in one session, one run at a time (nothing else runs). Method of P20/P23/P25 (bench.sh of P25:
# 3 repetitions in rotated order, arms wks server lxr-nonmoving lxr-nursery, heap 4x, ReadyToRun on, natural collections), then one
# traced run per LXR cell for the pause medians. Frames: base = P25 tip, f2, f4b = all workers on the trace, final = F2 default + F8.
# Then two A/B runs of the final frame: the wait after every pause (LXRWaitForLazy=1) and all workers on the trace (13).
# usage: bench-all.sh <tag-prefix>        results: /root/p25/results/<prefix>-<step>-*
P=${1:-s26}
R=/root/p25/results
for step in final base f2 f4b; do
  /root/p25/scripts/bench.sh /root/p25/frames/p26-$step-Release-linux $P-$step > $R/$P-$step-bench.out 2>&1
  echo "done $step $(date +%T)"
done
export FRAME=/root/p25/frames/p26-final-Release-linux
/root/p24/scripts/matrix.sh $R/$P-final-waitlazy-matrix.jsonl 3 "lxr-nonmoving lxr-nursery" "binarytrees requests listsuffix" DOTNET_LXRWaitForLazy=1 2> $R/$P-final-waitlazy-matrix.log
echo "done waitlazy $(date +%T)"
/root/p24/scripts/matrix.sh $R/$P-final-conc13-matrix.jsonl 3 "lxr-nonmoving lxr-nursery" "binarytrees requests listsuffix" DOTNET_LXRConcGCThreads=13 2> $R/$P-final-conc13-matrix.log
echo "done conc13 $(date +%T)"
/root/p25/scripts26/bench-mature.sh /root/p25/frames/p26-final-Release-linux $P-final > $R/$P-final-mature.out 2>&1
echo "done mature $(date +%T)"
echo done > $R/$P.done
