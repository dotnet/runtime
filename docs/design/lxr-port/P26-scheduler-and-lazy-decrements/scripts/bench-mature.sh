#!/bin/bash
# P26: the extra table of the task. The arms run as the collector runs by default: no LXREvacuateMature=0.
# lxr-defrag = no nursery copy + mature evacuation (the nonmoving arm with the mature copy on),
# lxr-mature = nursery copy + mature evacuation (the collector default).
# usage: bench-mature.sh <frame> <tag>      results: /root/p25/results/<tag>-mature-matrix.jsonl, <tag>-mature-table.txt
FRAME=$1; TAG=$2
R=/root/p25/results
export FRAME
/root/p24/scripts/matrix.sh $R/$TAG-mature-matrix.jsonl 3 "wks lxr-defrag lxr-mature" "binarytrees requests listsuffix" 2> $R/$TAG-mature-matrix.log
python3 /root/p24/scripts/matrix-table.py $R/$TAG-mature-matrix.jsonl > $R/$TAG-mature-table.txt
cat $R/$TAG-mature-table.txt
