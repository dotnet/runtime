#!/bin/bash
# Research-only: inclusive cost of selected functions in a perf recording (percent of all samples).
# usage: names.sh <name>
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
D=/root/gcbench/results/p15-perf/$1.data
$PERF report -i $D --stdio --children --sort symbol -g none 2>/dev/null | grep -v '^#' | grep -v '^$' > /tmp/perf-$1.txt
total=$($PERF report -i $D --stdio --sort comm -g none 2>/dev/null | grep -o 'Event count.*\|Samples:.*' | head -1)
echo "[$1] $total"
for sym in 'LxrWorkScheduler::Impl::WorkerLoop' 'LxrGcHeap::ExecuteScan' 'LxrGcHeap::Alloc$' 'LxrGcHeap::AllocationComplete' \
  'ImmixAllocationManager::AcquireBlock' 'LxrReferenceCounting::CompleteAllocation' 'LxrReferenceCounting::ReleaseEpoch' \
  'LxrReferenceCounting::ReclaimNursery' 'LxrReferenceCounting::Sweep$' 'LxrReferenceCounting::SweepCycles' \
  'LxrReferenceCounting::ProcessIncrementWork' 'LxrReferenceCounting::ProcessDecrement' 'LxrReferenceCounting::PrepareTrace' \
  'LxrReferenceCounting::DrainTraceInsidePause' 'LxrReferenceCounting::ProcessTraceObject' 'LxrReferenceCounting::GetHeapStatistics' \
  'LxrReferenceCounting::WalkObjects' 'LxrGcHeap::LeaseFinalizer' 'LxrVirtualArray.*Commit' 'ResetRangeQuiescentCore' \
  'SideMetadataManager::Load' 'SideMetadataManager::Store' 'ReferenceCountTable::DecrementWithDeath' 'LxrReferenceCounting::Reclaim$' \
  'LxrReferenceCounting::FixForwardingReaders' 'LxrReferenceCounting::RetireForwardedSources' 'LxrReferenceCounting::SetupRoots\|CaptureRoots'; do
  line=$(grep -E "\[\.\] $sym" /tmp/perf-$1.txt | head -1)
  [ -n "$line" ] && printf '%s\n' "$(echo "$line" | awk '{printf "  incl %6s  self %6s  ", $1, $2; for(i=4;i<=NF;i++) printf "%s ", $i; print ""}')"
done
