#!/bin/bash
# P22 (D2): run the native selectors of the LXR engine (P17 native-linux.sh) for one configuration.
# usage: run-native.sh <Debug|Release>
. "$(dirname "$0")/env.sh"
CFG=$1
bash $T/docs/design/lxr-port/P17-pause-cost-and-reserve/native-linux.sh $T $CFG $D/nat-$CFG > $D/nat-$CFG.txt 2>&1
echo "exit=$?" >> $D/nat-$CFG.txt
tail -6 $D/nat-$CFG.txt
grep -c -i -E 'fail|error' $D/nat-$CFG.txt
wc -l $D/nat-$CFG.txt
