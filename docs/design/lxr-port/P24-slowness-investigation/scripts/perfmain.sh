#!/bin/bash
# Perf: what does the main (allocating) thread do, and what do the workers do? usage: perfmain.sh <name>
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
D=/root/p24/results/perf/$1.data
echo "== threads"
$PERF report -i $D --stdio --no-children --sort pid -g none 2>/dev/null | grep -v '^#' | grep -v '^$' | head -12 | cut -c1-100
MAIN=$($PERF report -i $D --stdio --no-children --sort pid -g none 2>/dev/null | grep -v '^#' | grep -v '^$' | awk '{print $2}' | cut -d: -f1 | sort -n | head -1)
echo "== main thread tid=$MAIN: top symbols (self, percent of ALL samples)"
$PERF report -i $D --stdio --no-children --tid $MAIN --sort dso,symbol -g none 2>/dev/null | grep -v '^#' | grep -v '^$' | head -22 | cut -c1-140
echo "== all threads except main: top symbols"
$PERF report -i $D --stdio --no-children --sort dso,symbol -g none 2>/dev/null | grep -v '^#' | grep -v '^$' | head -14 | cut -c1-140
