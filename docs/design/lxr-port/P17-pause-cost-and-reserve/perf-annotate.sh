#!/bin/bash
# Show the hottest instructions of one function in a perf recording.
# usage: perf-annotate.sh <data-file> <symbol> [count]
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
$PERF annotate -i "$1" --stdio -s "$2" 2>/dev/null | grep -E '^ +[0-9]+\.[0-9]+ ' | sort -rn | head -n "${3:-12}"
