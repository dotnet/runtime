#!/bin/bash
# P28 (F9): the controls of the engine tests (selectors of P4). A control is a build define that breaks one behaviour; the selector
# must FAIL. usage: controls-engine28.sh <Debug|Release> [selector ...]     (uses /root/p25/scripts/one.sh and /root/p25/main)
# Each line: control, exit code, summary line of the selector.
CFG=${1:-Debug}; shift
SEL=${*:-default}
for control in "" LXR_F9_NO_PAUSE_RESET_CONTROL; do
  name=${control:-baseline}
  for sel in $SEL; do
    EXTRA=${control:+-D$control} OUTNAME=$name /root/p25/scripts/one.sh $CFG $sel > /root/p28/out/ctl-engine-$name.txt 2>&1
    code=$(grep -E '^exit=' /root/p28/out/ctl-engine-$name.txt | tail -1)
    echo "$CFG $name $sel $code :: $(grep -E '^(PASS|FAIL): [0-9]+/' /root/p28/out/ctl-engine-$name.txt | tail -1)"
  done
done
