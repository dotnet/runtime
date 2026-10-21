#!/bin/bash
# Repeat one cell many times, three builds interleaved, two parallel streams, to estimate the rate of an intermittent failure.
# usage: repeat-cell.sh <name> <workload> <factor> <mode> <reps-per-stream> <builds-spec>
NAME=$1; W=$2; F=$3; M=$4; N=$5; BUILDS=$6
OUT=/root/p17s2/results
rm -f $OUT/repeat-$NAME-s?.jsonl
CORES=("0-4" "5-9")
for s in 0 1; do
  taskset -c "${CORES[$s]}" python3 -u /root/p17s2/scripts/run-matrix.py --builds "$BUILDS" --workloads $W --factors $F --modes $M \
    --reps $N --seed "$NAME$s" --timeout 180 --output $OUT/repeat-$NAME-s$s.jsonl > $OUT/repeat-$NAME-s$s.log 2>&1 &
done
wait
cat $OUT/repeat-$NAME-s?.jsonl > $OUT/repeat-$NAME.jsonl
python3 - <<EOF
import json,collections
c=collections.Counter(); t=collections.Counter()
for l in open("$OUT/repeat-$NAME.jsonl"):
    r=json.loads(l); t[r["build"]]+=1
    if r.get("elapsed_ms") is None: c[r["build"]]+=1
for b in sorted(t): print(b, "failed", c[b], "of", t[b])
EOF
