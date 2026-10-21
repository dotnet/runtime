#!/bin/bash
# Research-only: summarize a perf recording. Self cost by symbol, then inclusive cost by symbol.
# usage: report.sh <name>
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
D=/root/gcbench/results/p15-perf/$1.data
echo "=== samples by thread (comm/tid) ==="
$PERF report -i $D --stdio --sort comm -g none --percent-limit 1 2>/dev/null | grep -v '^#' | grep -v '^$' | head -12
echo
echo "=== self cost by symbol ==="
$PERF report -i $D --stdio --no-children --sort dso,symbol -g none --percent-limit 0.8 2>/dev/null | grep -v '^#' | grep -v '^$' | cut -c1-170 | head -30
echo
echo "=== inclusive cost (children) by symbol ==="
$PERF report -i $D --stdio --children --sort symbol -g none --percent-limit 4 2>/dev/null | grep -v '^#' | grep -v '^$' | cut -c1-170 | head -50
