#!/bin/bash
# Reports of one perf recording. usage: perfrep.sh <name>
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
D=/root/p24/results/perf/$1.data
echo "== samples by thread (self, percent of all samples)"
$PERF report -i $D --stdio --no-children --sort pid -g none 2>&1 | grep -v '^#' | grep -v '^$' | head -14 | cut -c1-150
echo
echo "== inclusive (children) share of selected functions"
$PERF report -i $D --stdio --children --sort symbol -g none 2>/dev/null | grep -v '^#' | grep -v '^$' | cut -c1-170 \
  | grep -E "Resume|PublishTraceWork|FixForwardingReaders|ProcessIncrementWork|ProcessIncrement |ProcessDecrements|ProcessTraceWork|ProcessTraceObject|ReleaseEpoch|RetireCompletedWork|PrepareTrace|Suspend|WorkerLoop|Execute|NewWork|SubmitWork|Submit |RunPause|CollectLocked|FlushMutators|TryCopyNursery|DrainEpoch|finishRelocation|FinishRelocation|ScanOnGcThread|ExecuteScan|CaptureRoots|SweepCycles|RefreshBlock|Sweep" | head -40
