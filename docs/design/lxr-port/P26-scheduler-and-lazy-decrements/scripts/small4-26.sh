#!/bin/bash
# P26: list suffix on 4 processors with 1, 2 and 4 concurrent trace workers (final frame).
F=/root/p25/frames/p26-final-Release-linux
C=/root/p25/scripts26/cpucmp.sh
for c in 1 2 4; do
  echo "-- ConcGCThreads=$c"; taskset -c 0-3 $C $F listsuffix "lxr-nonmoving lxr-nursery" 2 DOTNET_LXRConcGCThreads=$c
done
