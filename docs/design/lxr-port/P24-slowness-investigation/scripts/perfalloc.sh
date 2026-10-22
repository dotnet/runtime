#!/bin/bash
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
for n in rq-nursery-k bt-nursery-k ls-nursery-batch-k; do
echo "=== $n (inclusive share of all samples, allocator side)"
$PERF report -i /root/p24/results/perf/$n.data --stdio --children --sort symbol -g none 2>/dev/null | grep -v '^#' | grep -v '^$' | cut -c1-120 \
  | grep -E "LxrReferenceCounting::Allocate|AcquireBlock|PrepareFreshBlock|PrepareRecycledHole|LxrGcHeap::Alloc|IsLineDead|ResetRangeQuiescentCore|LogOldValue|BarrierSlowPath|LxrReferenceCounting::LogBeforeWrite|DrainEpoch|WaitForCompletion|LxrGcHeap::CollectLocked" | head -12
done
