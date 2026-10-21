#!/bin/bash
# Research-only: read the mature-page count the port's policy sees at every decision.
# usage: gdb.sh <repo-root-as-WSL-path> <output-json>
set -u
SRC="$1"
OUT="$2"
NET=/root/lxr-batching-d2eccaea/after-Release-linux
mkdir -p /root/gcbench/trace
cp "$SRC/docs/design/lxr-port/P15-pauses-and-full-gcs/gdb-policy.py" /root/gcbench/trace/gdb-policy.py
sed -i 's/\r$//' /root/gcbench/trace/gdb-policy.py
cd /root/gcbench
heap=84
los=$(( heap / 8 )); [ $los -lt 8 ] && los=8
export LXR_GDB_OUTPUT="$OUT"
env -i PATH=/usr/bin:/bin LXR_GDB_OUTPUT="$OUT" DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
  DOTNET_GCPath=$NET/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=${3:-0} DOTNET_LXREvacuateMature=${4:-0} \
  DOTNET_LXRImmixInitialMB=$(( heap - los )) DOTNET_LXRLOSInitialMB=$los DOTNET_LXRManagedHeapHardLimitMB=$heap \
  gdb -batch -x /root/gcbench/trace/gdb-policy.py --args $NET/corerun net/gcbench.dll requests 32768 600000 > "$OUT.gdb.log" 2>&1
echo "gdb exit=$?"
tail -3 "$OUT.gdb.log"
