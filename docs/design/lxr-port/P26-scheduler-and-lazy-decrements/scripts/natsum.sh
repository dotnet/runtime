#!/bin/bash
# P26: summary of one native chain run (chain-native.sh <tag>): lines with a failed selector or a failed check.
# usage: natsum.sh <tag>
T=$1
cd /root/p25/out
for f in $T-nat-Debug.txt $T-nat-Release.txt $T-legacy.txt; do
  [ -f $f ] || { echo "$f missing"; continue; }
  echo "== $f: selectors with PASS: $(grep -c ':: PASS' $f), exit!=0: $(grep -c 'exit=[1-9]' $f)"
  grep -E 'exit=[1-9]|FAIL|failed=1' $f | head -12
  tail -2 $f
done
