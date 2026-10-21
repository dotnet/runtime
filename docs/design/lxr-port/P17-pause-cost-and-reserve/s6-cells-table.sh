#!/bin/bash
# The S6 managed cells on any build, one run each (nonmoving, the larger heap of S6_HEAP). Shows which cells an older
# collector cannot run.
# usage: s6-cells-table.sh <label> <frame-dir>
cd /root/p17
for c in finalizer-churn finalizer-churn-concurrent weak-churn resurrection prompt-collect pooled-reregister; do
  out=$(EXTRA_ENV="DOTNET_LXRImmixInitialMB=192 DOTNET_LXRLOSInitialMB=32 DOTNET_LXRManagedHeapHardLimitMB=224" TAILN=40 TMO=300 \
    bash cell1.sh $2 nonmoving $c Release /root/p17/fixtures 2>&1)
  res=$(echo "$out" | grep -E '^PASS|Fatal error|Out of memory|InvalidOperationException|Unhandled' | head -1 | cut -c1-150)
  echo "$1 $c :: ${res:-no result line}"
done
