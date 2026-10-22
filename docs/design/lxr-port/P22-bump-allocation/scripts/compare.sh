#!/bin/bash
# Direct wall-time comparison on ONE runtime build (the integrated branch): .NET Workstation GC, Server GC (2 heaps),
# and LXR (non-moving and nursery copy), one run after the other (no parallel streams), 3 repetitions in a rotated order.
# Heap = 4 times the smallest heap in which Workstation GC finishes (sizes.json of P14). Hard heap limit as in P14.
# P22 (D2): copy of the P20 script. The only change: ReadyToRun is on for every arm (LXR_READYTORUN, default 1; P20 had it off).
# usage: compare.sh <frame> <gcbench.dll> <out.jsonl>
set -u
FRAME=$1; PROGRAM=$2; OUT=$3
: > "$OUT"
declare -A SIZE=( [binarytrees]="16 0" [requests]="32768 600000" [listsuffix]="4096 800000" )
declare -A MIN=( [binarytrees]=10 [requests]=21 [listsuffix]=10 )
ARMS=(wks server lxr-nonmoving lxr-nursery)
for rep in 0 1 2; do
  for workload in binarytrees requests listsuffix; do
    heap=$(( ${MIN[$workload]} * 4 ))
    los=$(( heap / 8 )); [ $los -lt 8 ] && los=8
    for i in 0 1 2 3; do
      arm=${ARMS[$(( (i + rep) % 4 ))]}
      envs=(DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=${LXR_READYTORUN:-1} DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2)
      case $arm in
        wks)    envs+=(DOTNET_gcServer=0 DOTNET_GCHeapHardLimit=$(printf '%x' $(( heap * 1048576 )))) ;;
        server) envs+=(DOTNET_gcServer=1 DOTNET_GCHeapHardLimit=$(printf '%x' $(( heap * 1048576 )))) ;;
        lxr-*)  nursery=0; [ "$arm" = "lxr-nursery" ] && nursery=1
                envs+=(DOTNET_gcServer=0 DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1
                       DOTNET_LXREvacuateNursery=$nursery DOTNET_LXREvacuateMature=0
                       DOTNET_LXRImmixInitialMB=$(( heap - los )) DOTNET_LXRLOSInitialMB=$los
                       DOTNET_LXRManagedHeapHardLimitMB=$heap) ;;
      esac
      start=$(date +%s.%N)
      out=$(env -i PATH=/usr/bin:/bin "${envs[@]}" timeout 300 "$FRAME/corerun" "$PROGRAM" $workload ${SIZE[$workload]} run 2>&1)
      code=$?
      end=$(date +%s.%N)
      wall=$(printf '%.3f' "$(echo "$end - $start" | bc)")
      prog=$(echo "$out" | tail -1); case "$prog" in "{"*) ;; *) prog=null ;; esac
      echo "{\"rep\":$rep,\"workload\":\"$workload\",\"arm\":\"$arm\",\"heap_mib\":$heap,\"exit\":$code,\"wall_s\":$wall,\"prog\":$prog}" >> "$OUT"
    done
  done
done
python3 - "$OUT" <<'EOF'
import json, sys, statistics
rows = [json.loads(l) for l in open(sys.argv[1])]
for w in ("binarytrees", "requests", "listsuffix"):
    base = None
    sums = {r["prog"]["checksum"] for r in rows if r["workload"] == w and r["prog"]}
    print(f"{w} (checksums seen: {sorted(sums)})")
    for arm in ("wks", "server", "lxr-nonmoving", "lxr-nursery"):
        ok = [r for r in rows if r["workload"] == w and r["arm"] == arm and r["exit"] == 0 and r["prog"]]
        bad = sum(1 for r in rows if r["workload"] == w and r["arm"] == arm) - len(ok)
        if not ok:
            print(f"  {arm:14s} no finished run ({bad} failed)")
            continue
        wall = statistics.median(r["wall_s"] for r in ok)
        cpu = statistics.median(r["prog"]["cpu_ms"] for r in ok) / 1000
        gcm = statistics.median(r["prog"]["gc_ms"] for r in ok) / 1000
        rss = statistics.median(r["prog"]["peak_rss_kb"] for r in ok) / 1024
        if arm == "wks":
            base = (wall, cpu)
        ratio = f"wall {wall / base[0]:5.1f}x  cpu {cpu / base[1]:5.1f}x" if base else ""
        print(f"  {arm:14s} wall {wall:7.2f} s  cpu {cpu:7.2f} s  gc {gcm:6.2f} s  rss {rss:6.0f} MiB  runs {len(ok)} failed {bad}  {ratio}")
EOF
