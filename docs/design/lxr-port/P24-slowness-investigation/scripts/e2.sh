#!/bin/bash
# E2: concurrent trace off (latency mode Batch) against the default, list suffix.
for i in 1 2; do
  /root/p24/scripts/cell.sh listsuffix lxr-nursery e2-ls-nursery-default-$i
  /root/p24/scripts/cell.sh listsuffix lxr-nursery e2-ls-nursery-batch-$i GCBENCH_LATENCY=batch
done
for i in 1 2; do
  /root/p24/scripts/cell.sh listsuffix lxr-nonmoving e2-ls-nonmoving-default-$i
  /root/p24/scripts/cell.sh listsuffix lxr-nonmoving e2-ls-nonmoving-batch-$i GCBENCH_LATENCY=batch
done
