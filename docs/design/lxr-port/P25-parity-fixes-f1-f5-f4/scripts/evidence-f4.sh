#!/bin/bash
# P25 (F4): the evidence runs besides the matrix. usage: evidence-f4.sh <frame>
F=$1
R=/root/p25/results
{ echo "== concurrent trace limit (list suffix, nursery arm; workers, concurrent trace workers)"; /root/p25/scripts/limit-test.sh $F; } > $R/f4-conc-limit.txt 2>&1
{ echo "== worker counts, trace limit off"; /root/p25/scripts/limit-test2.sh $F; } > $R/f4-worker-sweep.txt 2>&1
{ echo "== thread CPU, requests nursery, 13 and 4 workers";
  /root/p25/scripts/threadcpu.sh $F requests lxr-nursery DOTNET_LXRGCThreads=13 DOTNET_LXRConcGCThreads=13
  /root/p25/scripts/threadcpu.sh $F requests lxr-nursery DOTNET_LXRGCThreads=4 DOTNET_LXRConcGCThreads=4; } > $R/f4-thread-cpu.txt 2>&1
echo finished >> $R/f4-thread-cpu.txt
