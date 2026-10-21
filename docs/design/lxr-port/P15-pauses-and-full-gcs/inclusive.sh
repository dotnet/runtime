#!/bin/bash
# Research-only: inclusive and self cost of named functions in one perf recording.
# usage: inclusive.sh <name> <regex-of-function-names>
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
D=/root/gcbench/results/p15-perf/$1.data
echo "=== $1: inclusive cost of selected functions ==="
$PERF report -i $D --stdio --children --sort symbol -g none --percent-limit 0.2 2>/dev/null \
  | grep -v '^#' | grep -v '^$' | cut -c1-190 | grep -E "$2"
echo
echo "=== $1: self cost inside samples whose call chain has $3 ==="
$PERF report -i $D --stdio --no-children --sort symbol -g none --parent "$3" -s parent,symbol --percent-limit 0.5 2>/dev/null \
  | grep -v '^#' | grep -v '^$' | cut -c1-190 | head -25
