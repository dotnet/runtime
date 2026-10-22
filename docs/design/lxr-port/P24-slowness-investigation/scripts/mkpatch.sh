#!/bin/bash
# The diagnostic variant of the collector differs from the repository sources in two small places. Save the difference.
# usage: mkpatch.sh <worktree-wsl-path> <out-file>
W=$1; OUT=$2
: > $OUT
for f in lxr_reference_counting.cpp lxr_gc.cpp; do
  diff -u --label a/src/coreclr/gc/$f --label b/src/coreclr/gc/$f <(tr -d '\r' < $W/src/coreclr/gc/$f) /root/p24/main/src/coreclr/gc/$f >> $OUT
done
wc -l $OUT
