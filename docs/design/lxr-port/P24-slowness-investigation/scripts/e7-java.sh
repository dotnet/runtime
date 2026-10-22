#!/bin/bash
# Java LXR verbose logs (one run per workload).
mkdir -p /root/p24/results/java
for w in binarytrees requests listsuffix; do
  /root/p24/scripts/java-lxr.sh $w /root/p24/results/java/lxr-$w.txt | tail -2
done
