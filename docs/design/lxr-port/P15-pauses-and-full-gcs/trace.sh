#!/bin/bash
# Research-only: per-collection traces of our port and of the Java LXR on the same programs and heaps as P14.
# usage: trace.sh <repo-root-as-WSL-path> <output-dir>
set -u
SRC="$1"
OUT="$2"
mkdir -p /root/gcbench/trace "$OUT"
cp "$SRC"/artifacts/P15/gctrace/gctrace.* /root/gcbench/trace/
NET=/root/lxr-batching-d2eccaea/after-Release-linux
JAVA=/root/lxr/head/mmtk-openjdk/repos/openjdk/build/linux-x86_64-normal-server-release/jdk/bin/java
cd /root/gcbench

net_env() { # heap MiB, evac-nursery, evac-mature
  local heap=$1 los=$(( $1 / 8 )); [ $los -lt 8 ] && los=8
  echo "DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
DOTNET_GCPath=$NET/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=$2 DOTNET_LXREvacuateMature=$3 \
DOTNET_LXRImmixInitialMB=$(( heap - los )) DOTNET_LXRLOSInitialMB=$los DOTNET_LXRManagedHeapHardLimitMB=$heap GCTRACE=1"
}

run_port() { # name workload size ops heap nursery mature
  local name=$1 w=$2 s=$3 o=$4 h=$5
  env -i PATH=/usr/bin:/bin $(net_env $h $6 $7) $NET/corerun trace/gctrace.dll $w $s $o > "$OUT/port-$name.txt" 2>&1
  echo "port $name exit=$?"
}

run_java() { # name workload size ops heap
  local name=$1 w=$2 s=$3 o=$4 h=$5
  env -i PATH=/usr/bin:/bin MMTK_PLAN=LXR MMTK_VERBOSE=3 $JAVA -server -XX:+UseThirdPartyHeap -Xms${h}M -Xmx${h}M -cp java GcBench $w $s $o > "$OUT/java-$name.txt" 2>&1
  echo "java $name exit=$?"
}

run_port binarytrees-4x binarytrees 16 0 40 0 0
run_port requests-4x requests 32768 600000 84 0 0
run_port listsuffix-4x listsuffix 4096 800000 40 0 0
run_java binarytrees-4x binarytrees 16 0 28
run_java requests-4x requests 32768 600000 52
run_java listsuffix-4x listsuffix 4096 800000 28
