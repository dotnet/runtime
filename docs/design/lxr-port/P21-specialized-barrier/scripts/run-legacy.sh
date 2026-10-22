#!/bin/bash
# P21 (D1): legacy P2.1-P3.4 suites (P17 legacy-linux.sh).
W=/mnt/c/Users/konradkokosa/.copilot/repos/copilot-worktrees/runtime-fork/kkokosa-expert-fortnight
sed 's/\r$//' $W/docs/design/lxr-port/P17-pause-cost-and-reserve/legacy-linux.sh > /root/d1/legacy-linux.sh
rm -rf /root/d1/legacy
bash /root/d1/legacy-linux.sh $W /root/d1/legacy > /root/d1/legacy.txt 2>&1
echo "exit=$?" >> /root/d1/legacy.txt
tail -15 /root/d1/legacy.txt
