#!/bin/bash
# Research-only: run the traced port in copying mode and print copy counters per collection.
# usage: copymode.sh <nursery> <mature>
NET=/root/lxr-batching-d2eccaea/after-Release-linux
cd /root/gcbench
H=84; los=$(( H / 8 )); [ $los -lt 8 ] && los=8
env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
  DOTNET_GCPath=$NET/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=$1 DOTNET_LXREvacuateMature=$2 \
  DOTNET_LXRImmixInitialMB=$(( H - los )) DOTNET_LXRLOSInitialMB=$los   DOTNET_LXRManagedHeapHardLimitMB=$H GCTRACE=1 ${3:+DOTNET_LXRDefragHeadroomPercent=$3} \
  $NET/corerun trace/gctrace.dll requests 32768 600000 > /root/gcbench/results/p15-copymode-$1$2.txt 2>&1
python3 - "$1$2" <<'EOF'
import json, sys
rows = [json.loads(l[len("trace_event="):]) for l in open("/root/gcbench/results/p15-copymode-%s.txt" % sys.argv[1]) if l.startswith("trace_event=")]
print("events", len(rows))
keys = ["full", "nursery_copied_objs", "nursery_copy_failures", "nursery_in_place_pinned_bytes", "mature_eligible_blocks", "mature_selected_blocks",
        "mature_pinned_blocks", "mature_estimated_copy_bytes", "mature_copied_objs", "mature_copy_failures", "remembered_entries", "blocks_released"]
print("i pause_ms " + " ".join(keys))
for i in [0, 1, 2, 3, 5, 8, 11, 15, 25, 40, len(rows) - 1]:
    if i < len(rows):
        e = rows[i]
        print(i, "%.0f" % e["pause_ms"], " ".join(str(e[k]) for k in keys))
EOF
