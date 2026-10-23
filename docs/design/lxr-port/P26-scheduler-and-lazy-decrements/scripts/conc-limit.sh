#!/bin/bash
# P26 (F2): the goal check of F2. A concurrent-trace worker limit of 3 (the Java LXR default) must not make list suffix
# slower or leave the trace unfinished. One run per setting and repetition, no tracing.
# usage: conc-limit.sh <frame> <reps> "<workloads>" "<arms>"
FRAME=$1; REPS=$2; WL=${3:-listsuffix}; ARMS=${4:-lxr-nonmoving lxr-nursery}
CELL=/root/p25/scripts26/cell.sh
for i in $(seq 1 $REPS); do
  for w in $WL; do
    for a in $ARMS; do
      $CELL $FRAME $w $a
      $CELL $FRAME $w $a DOTNET_LXRConcGCThreads=3
    done
  done
done
