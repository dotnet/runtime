#!/bin/bash
# P21 (D1): lean benchmark. Heap 4x, 3 workloads, nonmoving and nursery, 2 repetitions in two parallel streams (pinned
# cores), the two builds interleaved in a shuffled order. For information only.
#   tip = a1ddb3fd4a1 (Standard ABI barrier, ReadyToRun off), d1 = this branch (Specialized ABI barrier, ReadyToRun on)
set -u
W=/mnt/c/Users/konradkokosa/.copilot/repos/copilot-worktrees/runtime-fork/kkokosa-expert-fortnight
OUT=/root/d1/bench
mkdir -p $OUT; rm -f $OUT/wall-rep*.jsonl
BUILDS="tip=/root/rcf/frames/final,d1=/root/d1/frames/d1-Release-linux|DOTNET_ReadyToRun=1"
CORES=("0-4" "5-9")
for rep in 0 1; do
  taskset -c "${CORES[$rep]}" python3 -u $W/docs/design/lxr-port/P17-pause-cost-and-reserve/run-matrix.py --builds "$BUILDS" \
    --rep-indices $rep --seed d1 --factors 4 --modes nonmoving,nursery --program /root/gcbench/net/gcbench.dll \
    --output $OUT/wall-rep$rep.jsonl > $OUT/wall-rep$rep.log 2>&1 &
done
wait
cat $OUT/wall-rep?.jsonl > $OUT/wall.jsonl
echo "wall matrix finished: $(wc -l < $OUT/wall.jsonl) runs"
grep -c FAILED $OUT/wall-rep?.log
