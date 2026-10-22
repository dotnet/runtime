#!/bin/bash
# P22 (D2): the lean benchmark. Heap 4x, 3 workloads, nonmoving and nursery, 2 repetitions in two parallel streams (pinned
# cores), the two builds interleaved in a shuffled order. For information only.
#   d1 = the D1 tip 74dc9e84445 (notifying helpers), d2 = this branch (stock helpers)
set -u
. "$(dirname "$0")/env.sh"
OUT=$D/bench
mkdir -p $OUT; rm -f $OUT/wall-rep*.jsonl
BUILDS="d1=/root/d1/frames/d1-Release-linux|DOTNET_ReadyToRun=1,d2=$D/frames/d2-Release-linux|DOTNET_ReadyToRun=1"
CORES=("0-4" "5-9")
for rep in 0 1; do
  taskset -c "${CORES[$rep]}" python3 -u $W/docs/design/lxr-port/P17-pause-cost-and-reserve/run-matrix.py --builds "$BUILDS" \
    --rep-indices $rep --seed d2 --factors 4 --modes nonmoving,nursery --program /root/gcbench/net/gcbench.dll \
    --output $OUT/wall-rep$rep.jsonl > $OUT/wall-rep$rep.log 2>&1 &
done
wait
cat $OUT/wall-rep?.jsonl > $OUT/wall.jsonl
echo "wall matrix finished: $(wc -l < $OUT/wall.jsonl) runs"
grep -c FAILED $OUT/wall-rep?.log
python3 $W/docs/design/lxr-port/P22-bump-allocation/scripts/sum-bench.py $OUT/wall.jsonl
