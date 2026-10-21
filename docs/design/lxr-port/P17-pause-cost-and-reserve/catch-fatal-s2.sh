#!/bin/bash
# Research-only: repeat the traced listsuffix 4x nursery run of one build under gdb and print the message and the stack of the first Fatal.
# usage: catch-fatal-s2.sh <frame> <runs> <label> <cores>
FRAME=$1; RUNS=$2; LABEL=$3; CORES=$4
OUT=/root/p17s2/results/catch-fatal
mkdir -p $OUT
cd /root/gcbench
H=40; los=$(( H / 8 )); [ $los -lt 8 ] && los=8
for i in $(seq 1 $RUNS); do
  env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
    DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=1 DOTNET_LXREvacuateMature=0 \
    DOTNET_LXRImmixInitialMB=$(( H - los )) DOTNET_LXRLOSInitialMB=$los DOTNET_LXRManagedHeapHardLimitMB=$H \
    GCTRACE=1 GCTRACE_EXTRA=stamp_ns=243 \
    taskset -c $CORES timeout 600 gdb -batch -ex 'set pagination off' -ex 'set breakpoint pending on' \
    -ex 'handle SIGUSR1 nostop noprint pass' -ex 'handle SIGUSR2 nostop noprint pass' -ex 'handle SIGPWR nostop noprint pass' \
    -ex 'handle SIG34 nostop noprint pass' -ex 'handle SIG35 nostop noprint pass' -ex 'handle SIG36 nostop noprint pass' \
    -ex 'break GCToEEInterface::LogErrorToHost' -ex 'run' -ex 'x/s $rdi' -ex 'frame 1' -ex 'p this->m_engine.m_failure' -ex 'p this->m_engine.m_phase' -ex 'p this->m_shutdown' -ex 'p this->m_engine.m_trace_phase' -ex 'info threads' \
    --args $FRAME/corerun /root/gcbench/trace17/gctrace.dll listsuffix 4096 800000 run > $OUT/$LABEL-$i.log 2>&1
  if grep -q 'Breakpoint 1,' $OUT/$LABEL-$i.log; then echo "$LABEL run $i: FATAL CAUGHT"; else echo "$LABEL run $i: ok"; fi
done
