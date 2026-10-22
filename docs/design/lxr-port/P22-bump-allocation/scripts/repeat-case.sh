#!/bin/bash
# P22 (D2): repeat one lifecycle case N times with the Debug collector frame in P parallel loops (to oversubscribe the machine).
# Prints one line per run: exit code and the first PASS or assertion line. The S6 heap settings are used (as in run-managed.py).
# usage: repeat-case.sh <frame> <case> <nursery> <mature> <runs-per-loop> <loops> [env...]
. "$(dirname "$0")/env.sh"
FRAME=$1; CASE=$2; NUR=$3; MAT=$4; N=$5; P=$6; shift 6
mkdir -p $D/rep; rm -f $D/rep/$CASE-*.log
for j in $(seq 1 $P); do
  ( for i in $(seq 1 $N); do
      env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_gcServer=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_LXRLegacyCapacity=1 \
        DOTNET_LXRImmixInitialMB=192 DOTNET_LXRLOSInitialMB=32 DOTNET_LXRManagedHeapHardLimitMB=224 \
        DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXREvacuateNursery=$NUR DOTNET_LXREvacuateMature=$MAT "$@" \
        timeout 300 $FRAME/corerun $D/fixtures/fixture-scope5-Release/runtime-tests.dll --p7-lifecycle $CASE $FRAME/libclrlxrgc.so > $D/rep/$CASE-$j-$i.log 2>&1
      echo "loop $j run $i exit=$? $(grep -E 'PASS|Assertion|Fatal|E_FAIL' $D/rep/$CASE-$j-$i.log | head -1 | cut -c1-110)"
    done ) &
done
wait
