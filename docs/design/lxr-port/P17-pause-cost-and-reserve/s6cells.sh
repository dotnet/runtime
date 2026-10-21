#!/bin/bash
# usage: s6cells.sh <frame-dir> "<modes>" [cases] [Release|Debug]   the S6 managed cells with the larger heap
cd /root/p17
export EXTRA_ENV="DOTNET_LXRImmixInitialMB=192 DOTNET_LXRLOSInitialMB=32 DOTNET_LXRManagedHeapHardLimitMB=224"
CASES=${3:-"finalizer-churn finalizer-churn-concurrent weak-churn resurrection prompt-collect"}
for m in $2; do
  for c in $CASES; do
    echo "== $m $c"
    s=$(date +%s)
    TAILN=${TAILN:-3} TMO=${TMO:-600} bash cell1.sh $1 $m $c ${4:-Release} /root/p17/fixtures 2>&1 | grep -v '^   at ' | cut -c1-230
    echo "   seconds=$(( $(date +%s) - s ))"
  done
done
