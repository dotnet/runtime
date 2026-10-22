#!/bin/bash
# P25: run one selector without a control (must PASS) and with each control define (must FAIL).
# usage: controls-sel.sh <Debug|Release> <selector> <CONTROL_DEFINE...>
CFG=$1; SEL=$2; shift 2
for control in "" "$@"; do
  name=${control:-baseline}
  out=$(EXTRA=${control:+-D$control} OUTNAME=$name /root/p25/scripts/one.sh $CFG $SEL 2>&1)
  code=$(echo "$out" | grep -m1 '^exit=' | cut -d= -f2)
  summary=$(echo "$out" | grep -E '^(PASS|FAIL):' | tail -1)
  broken=$(echo "$out" | grep -E '^ +[0-9]+ FAIL: ' | grep -v -E 'checks$' | head -3 | sed 's/^ *[0-9]* FAIL: //' | tr '\n' '|')
  echo "$CFG $SEL $name exit=$code :: $summary :: $broken"
done
