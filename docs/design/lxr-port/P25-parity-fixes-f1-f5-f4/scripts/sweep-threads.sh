#!/bin/bash
# P25: one run of each cell for several worker counts. usage: sweep-threads.sh <frame> "<workloads>" "<arms>" "<thread counts>" [conc]
FRAME=$1; WL=$2; ARMS=$3; TS=($4); CONC=${5:-}
for t in "${TS[@]}"; do
  c=${CONC:-$(( t / 4 > 1 ? t / 4 : 1 ))}
  echo "threads=$t conc=$c"
  /root/p25/scripts/quick.sh $FRAME "$WL" "$ARMS" DOTNET_LXRGCThreads=$t DOTNET_LXRConcGCThreads=$c
done
