#!/bin/bash
# P25: the lean benchmark of P23 (method of compare.sh): heap 4x, ReadyToRun on, natural collections, 3 repetitions in a rotated
# order, arms wks server lxr-nonmoving lxr-nursery, 3 programs. One run at a time. Then one traced run per LXR cell for the pause medians.
# usage: bench.sh <frame> <tag>     results: /root/p25/results/<tag>-matrix.jsonl, <tag>-table.txt, <tag>-pauses.txt
FRAME=$1; TAG=$2
R=/root/p25/results
mkdir -p $R
export FRAME
/root/p24/scripts/matrix.sh $R/$TAG-matrix.jsonl 3 "wks server lxr-nonmoving lxr-nursery" "binarytrees requests listsuffix" 2> $R/$TAG-matrix.log
python3 /root/p24/scripts/matrix-table.py $R/$TAG-matrix.jsonl > $R/$TAG-table.txt
python3 - $R/$TAG-matrix.jsonl > $R/$TAG-checksums.txt <<'EOF'
import json, sys
rows = [json.loads(l) for l in open(sys.argv[1]) if l.strip()]
for w in ("binarytrees", "requests", "listsuffix"):
    s = sorted({r["prog"]["checksum"] for r in rows if r["workload"] == w and r["prog"]})
    bad = sum(1 for r in rows if r["workload"] == w and not r["prog"])
    print(w, "checksums", s, "failed runs", bad)
EOF
: > $R/$TAG-pauses.txt
for w in binarytrees requests listsuffix; do
  for a in lxr-nonmoving lxr-nursery; do
    /root/p25/scripts/trace1.sh $FRAME $w $a $R/$TAG-trace-$w-$a.txt > /dev/null 2>&1
    echo "== $w $a" >> $R/$TAG-pauses.txt
    python3 /root/p25/scripts/pausemed.py $R/$TAG-trace-$w-$a.txt >> $R/$TAG-pauses.txt
  done
done
cat $R/$TAG-table.txt $R/$TAG-checksums.txt $R/$TAG-pauses.txt
