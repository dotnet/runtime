#!/bin/bash
# Small matrix: heap x4, 3 workloads, nonmoving and nursery, 2 repetitions in two parallel streams (pinned cores).
# The builds run interleaved in a shuffled order inside each repetition (run-matrix.py of P17).
# usage: bench-wall.sh <p17-scripts-dir> <out-dir> <gcbench.dll> label=frame[,label=frame...]
#   example: bench-wall.sh docs/design/lxr-port/P17-pause-cost-and-reserve /tmp/out /root/gcbench/net/gcbench.dll \
#            acc=/frames/accepted,s6=/frames/s6tip,int=/frames/int-Release-linux
set -u
P17=$1; OUT=$2; PROGRAM=$3; BUILDS=$4
mkdir -p "$OUT"
rm -f "$OUT"/wall-rep*.jsonl
CORES=("0-4" "5-9")
for rep in 0 1; do
  taskset -c "${CORES[$rep]}" python3 -u "$P17/run-matrix.py" --builds "$BUILDS" --rep-indices $rep --seed int19 \
    --factors 4 --modes nonmoving,nursery --program "$PROGRAM" --output "$OUT/wall-rep$rep.jsonl" > "$OUT/wall-rep$rep.log" 2>&1 &
done
wait
cat "$OUT"/wall-rep?.jsonl > "$OUT/wall.jsonl"
echo "wall matrix finished: $(wc -l < "$OUT/wall.jsonl") runs"
