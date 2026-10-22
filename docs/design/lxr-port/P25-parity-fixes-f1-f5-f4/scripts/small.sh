#!/bin/bash
# P25: small machine check. The cells run with the process pinned to 1 and 2 processors (the worker count follows the
# processor count) and with one worker. usage: small.sh <frame>
F=$1
for cpus in 0 0,1 0-3; do
  echo "taskset $cpus"
  taskset -c $cpus /root/p25/scripts/quick.sh $F "binarytrees requests" "lxr-nonmoving lxr-nursery"
done
echo "one worker"
/root/p25/scripts/sweep-threads.sh $F "binarytrees" "lxr-nonmoving lxr-nursery" "1" 1
