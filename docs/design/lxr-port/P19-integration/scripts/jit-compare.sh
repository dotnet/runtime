#!/bin/bash
# Compare the JIT output of the demo program between two runtime frames (default GC).
# usage: jit-compare.sh <frame-a> <frame-b> <gcbench.dll>
# A frame is a flat runtime directory (see mkframe.sh). The JIT writes the code of every method that it compiles.
# The compared methods are the ones of the program itself (GcBench:*), because other threads write their own methods
# into the same file in an unpredictable order. Addresses are replaced before the comparison.
set -eu
A=$1; B=$2; PROG=$3
OUT=${OUT:-/tmp/jit-compare}
mkdir -p "$OUT"
for who in a b; do
  F=$A; [ $who = b ] && F=$B
  rm -f "$OUT/jit-$who.asm"
  env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_JitDisasm='*' DOTNET_JitDisasmAssemblies=gcbench \
    DOTNET_JitStdOutFile="$OUT/jit-$who.asm" DOTNET_JitDisasmWithGC=1 timeout 300 "$F/corerun" "$PROG" binarytrees 14 0 run > /dev/null 2>&1
  sed -E 's/0x[0-9A-Fa-f]{6,}/ADDR/g; s/; Total bytes of code.*//' "$OUT/jit-$who.asm" > "$OUT/jit-$who.norm"
  awk '/^; Assembly listing for method GcBench/{p=1} /^; Assembly listing for method / && !/GcBench/{p=0} p' "$OUT/jit-$who.norm" > "$OUT/jit-$who.gcbench"
  echo "$who: $(grep -c 'Assembly listing for method GcBench' "$OUT/jit-$who.gcbench") methods, $(wc -l < "$OUT/jit-$who.gcbench") lines, sha $(sha256sum "$OUT/jit-$who.gcbench" | cut -c1-16)"
done
cmp "$OUT/jit-a.gcbench" "$OUT/jit-b.gcbench" && echo "JIT output of the program methods is identical"
