#!/bin/bash
# P26: print the benchmark tables of one session. usage: show26.sh <prefix>
P=${1:-s26}
cd /root/p25/results
for t in final base f2 f4b; do
  echo "##### $t"
  cat $P-$t-table.txt
  cat $P-$t-checksums.txt
done
for t in final-waitlazy final-conc13; do
  echo "##### $t"
  python3 /root/p24/scripts/matrix-table.py $P-$t-matrix.jsonl
done
echo "##### mature"
cat $P-final-mature-table.txt
