#!/bin/bash
# P28: run one control against some selectors of P4.
# usage: control-one28.sh <Debug|Release> <control> <selector ...>
CFG=$1; control=$2; shift 2
mkdir -p /root/p28/out
for sel in "$@"; do
  EXTRA=-D$control OUTNAME=$control /root/p25/scripts/one.sh $CFG $sel > /root/p28/out/ctl-one-$control.txt 2>&1
  code=$(grep -E '^exit=' /root/p28/out/ctl-one-$control.txt | tail -1)
  echo "$CFG $control $sel $code :: $(grep -E '^(PASS|FAIL): [0-9]+/' /root/p28/out/ctl-one-$control.txt | tail -1)"
done
