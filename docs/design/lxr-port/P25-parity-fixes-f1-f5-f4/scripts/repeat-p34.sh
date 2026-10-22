#!/bin/bash
# P25: run the P3.4 completion-counter validation binary N times and list the failing checks.
# usage: repeat-p34.sh <dir-with-binary> <runs>
cd $1
for i in $(seq 1 $2); do
  ./lxr-completion-counter-validation > /tmp/p34-$i.log 2>&1
  echo "run $i exit $?"
done
grep -h FAIL /tmp/p34-*.log | sort | uniq -c
tail -3 /tmp/p34-1.log
