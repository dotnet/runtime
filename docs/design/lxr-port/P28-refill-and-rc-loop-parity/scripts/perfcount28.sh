#!/bin/bash
# P28: sample counts of the perf recordings. usage: perfcount28.sh
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
for f in /root/p28/perf/*.data; do
  n=$($PERF report -i $f --stdio --no-children --sort dso 2>/dev/null | grep -E '^# Samples' | head -1)
  echo "$(basename $f .data) $n"
done
