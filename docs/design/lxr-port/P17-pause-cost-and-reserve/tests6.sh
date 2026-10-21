#!/bin/bash
# usage: tests6.sh <label> <repo-wsl-path>   Release+Debug builds, native Release+Debug, managed, legacy; writes to /root/p17/out-<label>/
L=$1; S=$2
cd /root/p17
O=/root/p17/out-$L
mkdir -p $O
bash scripts/build-linux.sh $S Release $L > $O/build-Release.txt 2>&1
bash scripts/build-linux.sh $S Debug $L > $O/build-Debug.txt 2>&1
bash scripts/native-linux.sh $S Release /root/p17/native/$L-Release > $O/native-Release.txt 2>&1
bash scripts/native-linux.sh $S Debug /root/p17/native/$L-Debug > $O/native-Debug.txt 2>&1
python3 scripts/run-managed.py --frames Release=/root/p17/frames/$L-Release-linux,Debug=/root/p17/frames/$L-Debug-linux --modules Release=/root/p17/frames/$L-Release-linux/libclrlxrgc.so,Debug=/root/p17/frames/$L-Debug-linux/libclrlxrgc.so --fixtures /root/p17/fixtures --output /root/p17/managed/$L > $O/managed.txt 2>&1
bash scripts/legacy-linux.sh $S /root/p17/legacy/$L > $O/legacy.txt 2>&1
echo "tests6 $L done" > $O/done.txt
