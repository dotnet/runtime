#!/bin/bash
# P21 (D1): run the native selectors of the LXR engine (P17 native-linux.sh) for one configuration.
# usage: run-native.sh <Debug|Release>
W=/mnt/c/Users/konradkokosa/.copilot/repos/copilot-worktrees/runtime-fork/kkokosa-expert-fortnight
CFG=$1
bash $W/docs/design/lxr-port/P17-pause-cost-and-reserve/native-linux.sh $W $CFG /root/d1/nat-$CFG > /root/d1/nat-$CFG.txt 2>&1
echo "exit=$?" >> /root/d1/nat-$CFG.txt
tail -6 /root/d1/nat-$CFG.txt
grep -c -i -E 'fail|error' /root/d1/nat-$CFG.txt
wc -l /root/d1/nat-$CFG.txt
