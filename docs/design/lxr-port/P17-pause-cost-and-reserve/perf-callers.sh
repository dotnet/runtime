#!/bin/bash
# Show who calls a function in a perf recording (direct callers, percent of all samples, self time of the function).
# usage: perf-callers.sh <data-file> <symbol> [depth]
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
$PERF report -i "$1" --no-children -S "$2" -g caller,0.5,callee,function,percent --max-stack "${3:-3}" --stdio 2>/dev/null \
  | grep -v '^#' | grep -v '^$' | head -n 40
