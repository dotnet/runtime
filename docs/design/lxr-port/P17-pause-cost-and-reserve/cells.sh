#!/bin/bash
# usage: cells.sh <frame-dir> "<modes>" "<cases>" [Release|Debug fixture] [fixtures dir]
cd /root/p17
for m in $2; do
  for c in $3; do
    echo "== $m $c"
    TAILN=${TAILN:-2} bash cell1.sh $1 $m $c ${4:-Release} ${5:-/root/lxr-batching-d2eccaea} 2>&1 | cut -c1-220
  done
done
