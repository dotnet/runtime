#!/bin/bash
# P22 (D2): legacy P2.1-P3.4 suites (P17 legacy-linux.sh).
. "$(dirname "$0")/env.sh"
sed 's/\r$//' $W/docs/design/lxr-port/P17-pause-cost-and-reserve/legacy-linux.sh > $D/legacy-linux.sh
rm -rf $D/legacy
bash $D/legacy-linux.sh $W $D/legacy > $D/legacy.txt 2>&1
echo "exit=$?" >> $D/legacy.txt
tail -15 $D/legacy.txt
