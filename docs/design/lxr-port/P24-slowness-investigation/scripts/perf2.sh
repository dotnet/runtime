#!/bin/bash
# Perf recordings (user and kernel samples) of the cheap cells: requests nursery, binary trees nursery, and WKS for reference.
cd /root/p24
for spec in "requests lxr-nursery rq-nursery-k" "binarytrees lxr-nursery bt-nursery-k" "binarytrees lxr-nonmoving bt-nonmoving-k" "requests wks rq-wks-k" "binarytrees wks bt-wks-k"; do
  set -- $spec
  /root/p24/scripts/perfrun.sh $1 $2 $3 PERF_EVENT=cpu-clock > /dev/null 2>&1
  echo "recorded $3"
done
