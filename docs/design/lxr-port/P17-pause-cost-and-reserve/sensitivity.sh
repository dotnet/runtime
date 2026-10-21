#!/bin/bash
# Reserve sensitivity on one program. usage: sensitivity.sh <frame> <workload> <heapMiB> <nursery> <mature> "<floor:factor> ..."
FRAME=$1; W=$2; H=$3; NUR=$4; MAT=$5; VARIANTS=$6
for v in $VARIANTS; do
  floor=${v%%:*}; factor=${v##*:}
  out=$(bash /root/p17/scripts/run-one.sh $FRAME $W $H $NUR $MAT DOTNET_LXRReserveFloorPercent=$floor DOTNET_LXRReserveFactor=$factor | grep '^{')
  echo "floor=$floor factor=$factor $(echo "$out" | grep -o -E '"elapsed_ms":[0-9.]+|"gc_count":[0-9]+|"gc_ms":[0-9]+|"lxr_full":[0-9]+|"peak_rss_kb":[0-9]+|"checksum":[0-9]+' | tr '\n' ' ')"
done
