#!/bin/bash
# P21 (D1): store micro benchmark on the tip frame and the D1 frame (and the default GC of the D1 frame), interleaved,
# 5 runs each. Also the JIT disassembly size of the three store methods.
OUT=/root/d1/storebench-out
rm -rf $OUT; mkdir -p $OUT
BENCH=/root/d1/storebench/StoreBench.dll
run() {  # run <label> <frame> <mode lxr|default> <r2r> [extra env]
  local label=$1 frame=$2 mode=$3 r2r=$4; shift 4
  if [ "$mode" = lxr ]; then
    env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=$r2r DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
      DOTNET_GCPath=$frame/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=0 DOTNET_LXREvacuateMature=0 \
      DOTNET_LXRImmixInitialMB=80 DOTNET_LXRLOSInitialMB=20 DOTNET_LXRManagedHeapHardLimitMB=100 "$@" \
      taskset -c 2 timeout 300 $frame/corerun $BENCH 20000 5 2>&1 | tail -1
  else
    env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=$r2r "$@" taskset -c 2 timeout 300 $frame/corerun $BENCH 20000 5 2>&1 | tail -1
  fi
}
TIP=/root/rcf/frames/final
D1=/root/d1/frames/d1-Release-linux
for i in 1 2 3 4 5; do
  echo "tip-lxr $(run tip $TIP lxr 0)" >> $OUT/results.txt
  echo "d1-lxr $(run d1 $D1 lxr 1)" >> $OUT/results.txt
  echo "d1-default $(run d1 $D1 default 1)" >> $OUT/results.txt
  echo "tip-default $(run tip $TIP default 0)" >> $OUT/results.txt
done
cat $OUT/results.txt
# code size of the store methods (the JIT compiles them with full opts: TieredCompilation=0)
for pair in "tip:$TIP:0" "d1:$D1:1"; do
  IFS=: read label frame r2r <<< "$pair"
  env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=$r2r DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
    DOTNET_GCPath=$frame/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXRImmixInitialMB=80 DOTNET_LXRLOSInitialMB=20 DOTNET_LXRManagedHeapHardLimitMB=100 \
    DOTNET_JitDisasm="ScalarStores ArrayStores StructCopies" DOTNET_JitStdOutFile=$OUT/disasm-$label.txt \
    $frame/corerun $BENCH 200 1 > /dev/null 2>&1
  echo "== $label"; grep -E '^; Assembly listing|Total bytes of code' $OUT/disasm-$label.txt
done
