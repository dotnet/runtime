#!/bin/bash
# P26: small machine check of the final frame (and of the base frame for comparison). The process is pinned to 1, 2 and 4
# processors; the worker count and the trace limit follow the processor count. Every cell must end with the same checksum.
# usage: small26.sh <frame> ...
for F in "$@"; do
  echo "#### $(basename $F)"
  for cpus in 0 0,1 0-3; do
    echo "taskset $cpus"
    taskset -c $cpus /root/p25/scripts26/cell.sh $F "binarytrees requests listsuffix" "lxr-nonmoving lxr-nursery"
  done
done
