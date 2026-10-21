#!/bin/bash
# usage: lag1.sh <frame-dir> <mode> <objects> <ring> <payload> <disposeEvery> [threads] [heapMiB] [plain]
# One FinalizerLag run with natural collections and a hard limit (as the matrices). Prints the JSON line.
FRAME=$1; MODE=$2; OBJ=$3; RING=$4; PAY=$5; DISP=$6; THR=${7:-1}; HEAP=${8:-256}
case $MODE in nonmoving) N=0; M=0;; nursery) N=1; M=0;; mature) N=1; M=1;; esac
LOS=$(( HEAP / 8 )); [ $LOS -lt 8 ] && LOS=8
cd /root/p17/lag
env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
  DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=$N DOTNET_LXREvacuateMature=$M \
  DOTNET_LXRImmixInitialMB=$(( HEAP - LOS )) DOTNET_LXRLOSInitialMB=$LOS DOTNET_LXRManagedHeapHardLimitMB=$HEAP $EXTRA_ENV \
  timeout ${TMO:-300} $FRAME/corerun /root/p17/lag/FinalizerLag.dll $OBJ $RING $PAY $DISP $THR ${9:-} 2>&1 | tail -${TAILN:-3} | cut -c1-6000
echo "exit=${PIPESTATUS[0]}"
