#!/bin/bash
# P21 (D1): repeat one lifecycle case N times with the Debug collector frame; print the exit code of each run.
# usage: repeat-case.sh <frame> <case> <nursery> <mature> <count> [env...]
FRAME=$1; CASE=$2; NUR=$3; MAT=$4; N=$5; shift 5
for i in $(seq 1 $N); do
  env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_gcServer=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_LXRLegacyCapacity=1 \
    DOTNET_LXRImmixInitialMB=192 DOTNET_LXRLOSInitialMB=32 DOTNET_LXRManagedHeapHardLimitMB=224 \
    DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXREvacuateNursery=$NUR DOTNET_LXREvacuateMature=$MAT "$@" \
    timeout 300 $FRAME/corerun /root/d1/fixtures/fixture-scope5-Release/runtime-tests.dll --p7-lifecycle $CASE $FRAME/libclrlxrgc.so > /root/d1/rep-$CASE-$i.log 2>&1
  echo "run $i exit=$? $(grep -E 'PASS|Fatal' /root/d1/rep-$CASE-$i.log | head -1 | cut -c1-80)"
done
