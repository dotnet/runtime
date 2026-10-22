#!/bin/bash
# P21 (D1): run binary trees once with LXR (ReadyToRun enabled) or the default GC.
# usage: smoke-r2r.sh <frame> [lxr|default] [extra env assignments...]
FRAME=$1; MODE=${2:-lxr}; shift 2 || true
cd /root/gcbench
if [ "$MODE" = lxr ]; then
  env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
    DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=0 DOTNET_LXREvacuateMature=0 \
    DOTNET_LXRImmixInitialMB=80 DOTNET_LXRLOSInitialMB=20 DOTNET_LXRManagedHeapHardLimitMB=100 "$@" \
    timeout 300 $FRAME/corerun net/gcbench.dll binarytrees 16 0 run 2>&1 | tail -8
else
  env -i PATH=/usr/bin:/bin "$@" timeout 300 $FRAME/corerun net/gcbench.dll binarytrees 16 0 run 2>&1 | tail -8
fi
