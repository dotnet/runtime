#!/bin/bash
# P25: worker counts with the concurrent trace limit off (conc = workers). usage: limit-test2.sh <frame>
F=$1
S=/root/p25/scripts/sweep-threads.sh
for t in 4 8 13; do
  $S $F "binarytrees requests listsuffix" "lxr-nonmoving lxr-nursery" "$t" $t
done
