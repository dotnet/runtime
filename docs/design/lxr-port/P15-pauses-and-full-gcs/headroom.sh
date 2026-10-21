#!/bin/bash
# Research-only: copying mode with several defrag headroom percentages.
for p in 30 50; do
  echo "HEADROOM=$p"
  bash /root/gcbench/copymode.sh 1 1 $p
done
