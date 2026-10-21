#!/bin/bash
# P17 S7 wall-time matrix: integrated tip (int) against the S7 tip (s7), heap x4, three workloads, modes non-moving and
# nursery copy, two repetitions in two parallel streams on pinned cores, builds interleaved in a shuffled order.
# usage: bench-wall-s7.sh <out-dir> <int-frame> <s7-frame>
set -u
OUT=$1; INT=$2; S7=$3
mkdir -p $OUT
rm -f $OUT/wall-rep*.jsonl
B="int=$INT,s7=$S7"
CORES=("0-4" "5-9")
for rep in 0 1; do
  taskset -c "${CORES[$rep]}" python3 -u /root/p17s6/scripts/run-matrix.py --builds "$B" --rep-indices $rep --seed s7pins \
    --factors 4 --modes nonmoving,nursery --program /root/gcbench/net/gcbench.dll --output $OUT/wall-rep$rep.jsonl > $OUT/wall-rep$rep.log 2>&1 &
done
wait
cat $OUT/wall-rep?.jsonl > $OUT/wall.jsonl
echo "wall matrix finished: $(wc -l < $OUT/wall.jsonl) runs"
