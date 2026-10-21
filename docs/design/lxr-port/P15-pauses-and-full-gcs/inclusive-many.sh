#!/bin/bash
# Research-only: inclusive cost of selected functions for three recordings.
PATTERN='Sweep|ReclaimNursery|WalkObjects|ReleaseEpoch|ExecuteScan|GetHeapStatistics|WorkerLoop|TryReleaseIfRcDead|TryFindNextHole|LeaseFinalizer|SweepCycles|CollectLocked|AllocationComplete|ProcessTraceObject|ProcessIncrementWork|ProcessDecrements|Drain|SideMetadataManager::(Load|Store|GetLocation)$|Reclaim$'
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
for n in binarytrees-nonmoving requests-nonmoving; do
  echo "=== $n ==="
  $PERF report -i /root/gcbench/results/p15-perf/$n.data --stdio --children --sort symbol -g none --percent-limit 0.2 2>/dev/null \
    | grep -v '^#' | grep -v '^$' | cut -c1-110 | grep -E "$PATTERN"
done
