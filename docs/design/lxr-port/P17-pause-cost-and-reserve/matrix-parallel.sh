#!/bin/bash
# Run the P17 matrix as three parallel streams, one repetition each, every stream pinned to its own cores.
# The builds inside one repetition are interleaved in a shuffled order, so they see the same conditions.
# usage: matrix-parallel.sh <name> <builds-spec> [extra run-matrix.py args]
set -u
NAME=$1
BUILDS=$2
shift 2
OUT=/root/p17/results
mkdir -p "$OUT"
rm -f "$OUT/matrix-$NAME-rep"*.jsonl
CORES=("0-4" "5-9" "10-14")
for rep in 0 1 2; do
  taskset -c "${CORES[$rep]}" python3 -u /root/p17/scripts/run-matrix.py --builds "$BUILDS" --rep-indices $rep \
    --seed "$NAME" --output "$OUT/matrix-$NAME-rep$rep.jsonl" "$@" > "$OUT/matrix-$NAME-rep$rep.log" 2>&1 &
done
wait
cat "$OUT"/matrix-$NAME-rep?.jsonl > "$OUT/matrix-$NAME.jsonl"
echo "matrix $NAME finished: $(wc -l < "$OUT/matrix-$NAME.jsonl") runs"
