#!/bin/bash
# Run the Java LXR (mmtk-openjdk 0682434ae725, mmtk-core 304ce69d43aa, OpenJDK 7caf8f7d19b) on the GcBench.java workloads with the
# verbose GC log. Heap = 4 times the minimum heap of Java Serial GC (P14 sizes.json: binarytrees 7, requests 13, listsuffix 7 MiB).
# usage: java-lxr.sh <workload> <out-file> [NAME=VALUE ...]     (extra pairs go to the JVM environment, for example MMTK_NURSERY_BLOCKS=...)
W=$1; OUT=$2; shift 2
JAVA=/root/lxr/head/mmtk-openjdk/repos/openjdk/build/linux-x86_64-normal-server-release/jdk/bin/java
declare -A SIZE=( [binarytrees]="16 0" [requests]="32768 600000" [listsuffix]="4096 800000" )
declare -A HEAP=( [binarytrees]=28 [requests]=52 [listsuffix]=28 )
start=$(date +%s.%N)
env -i PATH=/usr/bin:/bin MMTK_PLAN=LXR MMTK_VERBOSE=${MMTK_VERBOSE:-3} "$@" timeout 300 $JAVA -server -XX:+UseThirdPartyHeap -Xms${HEAP[$W]}M -Xmx${HEAP[$W]}M \
  -cp /root/gcbench/java GcBench $W ${SIZE[$W]} run > $OUT 2>&1
end=$(date +%s.%N)
echo "wall_s=$(printf '%.3f' "$(echo "$end - $start" | bc)")" >> $OUT
tail -3 $OUT | cut -c1-400
