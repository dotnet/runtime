#!/bin/bash
# One natural-collection run, with the settings of the P23 compare.sh (ReadyToRun on, heap 4x the Workstation minimum).
# usage: run1.sh <frame> <program.dll> <workload> <arm: wks|server|lxr-nonmoving|lxr-nursery|lxr-defrag|lxr-mature> [NAME=VALUE ...]
# lxr-defrag = no nursery copy + mature evacuation; lxr-mature = nursery copy + mature evacuation (the defaults of the collector).
# Extra NAME=VALUE pairs are added to the environment (for example GCTRACE=1). Output: the program output, then
# "wall_s=<seconds>" on the last line. Nothing runs in parallel with it.
FRAME=$1; PROGRAM=$2; workload=$3; arm=$4
shift 4
declare -A SIZE=( [binarytrees]="16 0" [requests]="32768 600000" [listsuffix]="4096 800000" )
declare -A MIN=( [binarytrees]=10 [requests]=21 [listsuffix]=10 )
heap=$(( ${MIN[$workload]} * 4 ))
los=$(( heap / 8 )); [ $los -lt 8 ] && los=8
envs=(DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=1 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2)
case $arm in
  wks)    envs+=(DOTNET_gcServer=0 DOTNET_GCHeapHardLimit=$(printf '%x' $(( heap * 1048576 )))) ;;
  server) envs+=(DOTNET_gcServer=1 DOTNET_GCHeapHardLimit=$(printf '%x' $(( heap * 1048576 )))) ;;
  lxr-*)  nursery=0; mature=0
          [ "$arm" = "lxr-nursery" ] && nursery=1
          [ "$arm" = "lxr-defrag" ] && mature=1
          [ "$arm" = "lxr-mature" ] && { nursery=1; mature=1; }
          envs+=(DOTNET_gcServer=0 DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1
                 DOTNET_LXREvacuateNursery=$nursery DOTNET_LXREvacuateMature=$mature
                 DOTNET_LXRImmixInitialMB=$(( heap - los )) DOTNET_LXRLOSInitialMB=$los
                 DOTNET_LXRManagedHeapHardLimitMB=$heap) ; [ -n "${P24_NOLIMIT:-}" ] && envs=("${envs[@]/DOTNET_LXRManagedHeapHardLimitMB=*/DOTNET_LXRNoLimit=1}") ;;
esac
start=$(date +%s.%N)
env -i PATH=/usr/bin:/bin "${envs[@]}" "$@" timeout 600 "$FRAME/corerun" "$PROGRAM" $workload ${P24_ARGS:-${SIZE[$workload]}} run 2>&1
end=$(date +%s.%N)
echo "wall_s=$(printf '%.3f' "$(echo "$end - $start" | bc)")"
