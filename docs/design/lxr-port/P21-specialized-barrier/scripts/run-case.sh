#!/bin/bash
# P21 (D1): run one lifecycle case of the managed fixture.
# usage: run-case.sh <frame> <case> [nursery 0|1] [mature 0|1] [env assignments...]
FRAME=$1; CASE=$2; NUR=${3:-0}; MAT=${4:-0}; shift 4 || true
env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_gcServer=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_LXRLegacyCapacity=1 \
  DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXREvacuateNursery=$NUR DOTNET_LXREvacuateMature=$MAT "$@" \
  timeout 300 $FRAME/corerun /root/d1/fixtures/fixture-scope5-Release/runtime-tests.dll --p7-lifecycle $CASE $FRAME/libclrlxrgc.so 2>&1 | tail -${TAILN:-25}
