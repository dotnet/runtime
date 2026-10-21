#!/bin/bash
# Research-only: catch the port's Fatal message and the stack that raised it.
NET=/root/lxr-batching-d2eccaea/after-Release-linux
cd /root/gcbench
H=84; los=$(( H / 8 )); [ $los -lt 8 ] && los=8
env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
  DOTNET_GCPath=$NET/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=0 DOTNET_LXREvacuateMature=0 \
  DOTNET_LXRImmixInitialMB=$(( H - los )) DOTNET_LXRLOSInitialMB=$los DOTNET_LXRManagedHeapHardLimitMB=$H \
  timeout 400 gdb -batch -ex 'set pagination off' -ex 'set breakpoint pending on' \
  -ex 'handle SIGUSR1 nostop noprint pass' -ex 'handle SIGUSR2 nostop noprint pass' -ex 'handle SIGPWR nostop noprint pass' \
  -ex 'handle SIG34 nostop noprint pass' -ex 'handle SIG35 nostop noprint pass' -ex 'handle SIG36 nostop noprint pass' \
  -ex 'break GCToEEInterface::LogErrorToHost' -ex 'run' -ex 'x/s $rdi' -ex 'bt 10' \
  --args $NET/corerun net/gcbench.dll requests 32768 250000 2>&1 | grep -v -E '^\[New Thread|^warning|CRC mismatch|^$' | tail -25 | cut -c1-220
