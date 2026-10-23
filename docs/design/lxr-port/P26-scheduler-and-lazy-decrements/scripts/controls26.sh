#!/bin/bash
# P26: run one selector without a control (must PASS) and with each control define (must FAIL). Prints the lines that the
# test itself prints (names that start with f2_, f4b_, f8_) so the numbers of the control are visible.
# usage: controls26.sh <Debug|Release> <selector> <CONTROL_DEFINE...>
CFG=$1; SEL=$2; shift 2
for control in "" "$@"; do
  name=${control:-baseline}
  EXTRA=${control:+-D$control} OUTNAME=$name /root/p25/scripts/one.sh $CFG $SEL > /root/p25/out/ctl-$CFG-$name.txt 2>&1
  code=$(grep -m1 '^exit=' /root/p25/out/ctl-$CFG-$name.txt | cut -d= -f2)
  summary=$(grep -E '^(PASS|FAIL):' /root/p25/out/ctl-$CFG-$name.txt | tail -1)
  broken=$(grep -E '^ +[0-9]+ FAIL: ' /root/p25/out/ctl-$CFG-$name.txt | grep -v -E 'checks$' | head -3 | sed 's/^ *[0-9]* FAIL: //' | tr '\n' '|')
  echo "$CFG $SEL $name exit=$code :: $summary :: $broken"
  grep -h -E '^(f2_|f4b_|f8_)' /root/p25/out/one-$CFG-$name/run.log 2>/dev/null | head -${LINES_MAX:-6}
done
