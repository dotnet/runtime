#!/bin/bash
# P21 (D1): run the native barrier test and its controls (Linux).
W=/mnt/c/Users/konradkokosa/.copilot/repos/copilot-worktrees/runtime-fork/kkokosa-expert-fortnight
sed 's/\r$//' $W/docs/design/lxr-port/P21-specialized-barrier/native-barrier-linux.sh > /root/d1/native-barrier-linux.sh
bash /root/d1/native-barrier-linux.sh $W /root/d1/bt-linux > /root/d1/bt-linux.txt 2>&1
echo "exit=$?" >> /root/d1/bt-linux.txt
tail -25 /root/d1/bt-linux.txt
