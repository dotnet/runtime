#!/bin/bash
# P25: run every legacy scheduler validation binary N times (the suites P3.1 to P3.4 test the scheduler) and count the failures.
# The binaries come from the last legacy run (/root/p25/out/legacy). usage: repeat-legacy.sh <runs>
N=$1
cd /root/p25/out/legacy
for d in run-lxr-work-packet-validation run-lxr-pause-validation run-lxr-deferred-bucket-validation run-lxr-completion-counter-validation; do
  bin=$(ls $d | grep -E '^lxr-.*validation$' | head -1)
  fails=0
  for i in $(seq 1 $N); do
    ./$d/$bin > /tmp/rl.log 2>&1 || { fails=$((fails + 1)); grep -m1 '^FAIL' /tmp/rl.log >> /tmp/rl-fails.txt; }
  done
  echo "$d: $fails failures in $N runs"
done
[ -f /tmp/rl-fails.txt ] && sort /tmp/rl-fails.txt | uniq -c && rm /tmp/rl-fails.txt
