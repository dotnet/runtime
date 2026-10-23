#!/bin/bash
# P26: list suffix nonmoving on 2 processors for each step and for settings of the final frame.
# usage: small2-26.sh <reps>
N=${1:-2}
D=/root/p25/frames
C=/root/p25/scripts26/cpucmp.sh
for f in base f2 f4b final; do
  echo "-- $f"; taskset -c 0,1 $C $D/p26-$f-Release-linux listsuffix lxr-nonmoving $N
done
echo "-- final WaitForLazy=1"; taskset -c 0,1 $C $D/p26-final-Release-linux listsuffix lxr-nonmoving $N DOTNET_LXRWaitForLazy=1
echo "-- final ConcGCThreads=2"; taskset -c 0,1 $C $D/p26-final-Release-linux listsuffix lxr-nonmoving $N DOTNET_LXRConcGCThreads=2
