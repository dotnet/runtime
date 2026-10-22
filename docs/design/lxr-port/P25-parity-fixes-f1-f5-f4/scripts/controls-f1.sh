#!/bin/bash
# P25 (F1): the controls of the trace-packet tests. Each control is a build define that breaks one behaviour; the selector
# --f1-trace must then FAIL. The baseline (no define) must PASS.
# usage: controls-f1.sh [Debug|Release]
CFG=${1:-Release}
for control in "" LXR_F1_QUEUE_ALL_CONTROL LXR_F1_SKIP_ALL_CONTROL LXR_F1_NO_ROOT_TRACE_CONTROL LXR_D2_NO_MARK_CONTROL; do
  name=${control:-baseline}
  out=$(EXTRA=${control:+-D$control} OUTNAME=$name /root/p25/scripts/one.sh $CFG --f1-trace 2>&1)
  code=$(echo "$out" | grep -m1 '^exit=' | cut -d= -f2)
  summary=$(echo "$out" | grep -E '^(PASS|FAIL):' | tail -1)
  broken=$(echo "$out" | grep -E '^ +[0-9]+ FAIL: ' | head -3 | sed 's/^ *[0-9]* FAIL: //' | tr '\n' '|')
  echo "$CFG $name exit=$code :: $summary :: $broken"
done
