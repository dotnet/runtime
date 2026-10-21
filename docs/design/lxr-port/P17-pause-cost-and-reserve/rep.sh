#!/bin/bash
# usage: rep.sh <frame-dir> <mode> <case> <count> [Release|Debug fixture]   repeat one S6 cell, print the exit codes
cd /root/p17
export EXTRA_ENV="DOTNET_LXRImmixInitialMB=192 DOTNET_LXRLOSInitialMB=32 DOTNET_LXRManagedHeapHardLimitMB=224"
ok=0; bad=0
for i in $(seq 1 $4); do
  out=$(TAILN=8 TMO=300 bash cell1.sh $1 $2 $3 ${5:-Release} /root/p17/fixtures 2>&1)
  if echo "$out" | grep -q '^PASS'; then ok=$((ok+1)); else bad=$((bad+1)); echo "--- failure $i"; echo "$out" | cut -c1-200 | head -12; fi
done
echo "ok=$ok bad=$bad"
