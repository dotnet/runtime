#!/bin/bash
# P28: show the A/B result files. usage: showab28.sh
cd /root/p28/results
for f in ab-base-f9a ab-f9a-f9c ab-f9c-f9d ab-f10b ab-f10b-f10c ab-f10c-f10d ab-f10d-f10e; do
  echo "=== $f"
  cat $f.txt
done
