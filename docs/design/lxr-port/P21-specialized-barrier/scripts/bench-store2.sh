#!/bin/bash
# P21 (D1): the D1 frame with ReadyToRun off, store micro benchmark (is the array store difference from R2R code?).
BENCH=/root/d1/storebench/StoreBench.dll
D1=/root/d1/frames/d1-Release-linux
for i in 1 2 3; do
  echo "d1-lxr-r2r-off $(env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
      DOTNET_GCPath=$D1/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=0 DOTNET_LXREvacuateMature=0 \
      DOTNET_LXRImmixInitialMB=80 DOTNET_LXRLOSInitialMB=20 DOTNET_LXRManagedHeapHardLimitMB=100 \
      taskset -c 2 timeout 300 $D1/corerun $BENCH 20000 5 2>&1 | tail -1)"
done
