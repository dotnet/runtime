#!/bin/bash
# P25: the whole Linux test chain on the synced tree: native selectors (Debug and Release), legacy P2.1-P3.4 suites.
# Results go to /root/p25/out/<tag>-*.txt. usage: chain-native.sh <tag>
TAG=$1
R=/root/p25
for cfg in Debug Release legacy; do
  $R/scripts/tests-native.sh $cfg > $R/out/$TAG-$cfg.summary 2>&1
  cp $R/out/nat-$cfg.txt $R/out/$TAG-nat-$cfg.txt 2>/dev/null
  cp $R/out/legacy.txt $R/out/$TAG-legacy.txt 2>/dev/null
done
echo done > $R/out/$TAG.done
