#!/bin/bash
# Extra repetitions of some cells for two builds, three pinned parallel streams (two repetitions each).
# usage: extra-cells.sh <name> <builds-spec> <workloads> <factors> <modes> [first-rep-index]
set -u
NAME=$1; BUILDS=$2; WL=$3; FAC=$4; MODES=$5; FIRST=${6:-3}
OUT=/root/p17s6/results
rm -f $OUT/matrix-$NAME-rep*.jsonl
CORES=("0-4" "5-9" "10-14")
for s in 0 1 2; do
  a=$((FIRST + s)); b=$((FIRST + s + 3))
  taskset -c "${CORES[$s]}" python3 -u /root/p17s6/scripts/run-matrix.py --builds "$BUILDS" --rep-indices $a,$b \
    --seed "$NAME" --workloads "$WL" --factors "$FAC" --modes "$MODES" --output "$OUT/matrix-$NAME-rep$s.jsonl" \
    > "$OUT/matrix-$NAME-rep$s.log" 2>&1 &
done
wait
cat $OUT/matrix-$NAME-rep?.jsonl > $OUT/matrix-$NAME.jsonl
echo "extra $NAME finished: $(wc -l < $OUT/matrix-$NAME.jsonl) runs"
