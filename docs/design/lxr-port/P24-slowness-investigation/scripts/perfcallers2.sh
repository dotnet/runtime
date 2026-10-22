#!/bin/bash
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
D=/root/p24/results/perf/${1:-ls-nursery-k}.data
for S in "LxrReferenceCounting::NewWork" "LxrReferenceCounting::QueueTrace" "LxrReferenceCounting::Resume"; do
echo "== callers of $S (inclusive, caller chain)"
$PERF report -i $D --stdio --children -S "$S" -g caller,0.5,callee,function,percent --max-stack 8 2>/dev/null | grep -v '^#' | grep -v '^$' | head -30 | cut -c1-150
done
