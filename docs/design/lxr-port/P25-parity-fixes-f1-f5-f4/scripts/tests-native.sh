#!/bin/bash
# P25: native selectors (P17 native-linux.sh) and the legacy P2.1-P3.4 suites on the synced tree /root/p25/main.
# usage: tests-native.sh <Debug|Release|legacy>
CFG=$1
R=/root/p25
mkdir -p $R/out
if [ "$CFG" = legacy ]; then
  bash $R/main/docs/design/lxr-port/P17-pause-cost-and-reserve/legacy-linux.sh $R/main $R/out/legacy > $R/out/legacy.txt 2>&1
  echo "exit=$?" >> $R/out/legacy.txt
  tail -12 $R/out/legacy.txt
else
  bash $R/main/docs/design/lxr-port/P17-pause-cost-and-reserve/native-linux.sh $R/main $CFG $R/out/nat-$CFG > $R/out/nat-$CFG.txt 2>&1
  echo "exit=$?" >> $R/out/nat-$CFG.txt
  tail -8 $R/out/nat-$CFG.txt
  echo "lines with fail/error: $(grep -c -i -E 'fail|error' $R/out/nat-$CFG.txt)"
fi
