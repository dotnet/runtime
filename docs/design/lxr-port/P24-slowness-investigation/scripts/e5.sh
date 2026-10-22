#!/bin/bash
# E5: requests, non-moving: the knobs of the emergency (full collection) decision.
for p in 5 10 12 15; do
  /root/p24/scripts/cell.sh requests lxr-nonmoving e5-rq-nonmoving-stop$p DOTNET_LXRRCStopPercent=$p
done
# E5b: the split between the Immix space and the large-object space (the capacity of the policy is both), heap 84 MiB, hard limit kept.
for split in "76 8" "72 12" "64 20"; do
  set -- $split
  /root/p24/scripts/cell.sh requests lxr-nonmoving e5-rq-nonmoving-split$1-$2 DOTNET_LXRImmixInitialMB=$1 DOTNET_LXRLOSInitialMB=$2
done
# E6: list suffix, concurrent trace off, size of the young allocation between two pauses.
for y in 4 8 16 32; do
  /root/p24/scripts/cell.sh listsuffix lxr-nonmoving e6-ls-nonmoving-batch-young$y GCBENCH_LATENCY=batch DOTNET_LXRYoungLimitMB=$y
  /root/p24/scripts/cell.sh listsuffix lxr-nursery e6-ls-nursery-batch-young$y GCBENCH_LATENCY=batch DOTNET_LXRYoungLimitMB=$y
done
