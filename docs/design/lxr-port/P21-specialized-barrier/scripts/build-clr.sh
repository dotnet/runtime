#!/bin/bash
# Build the Linux Release runtime (clr: runtime, JIT, CoreLib with ReadyToRun) with the LXR collector in the D1 tree.
# usage: build-clr.sh <Release|Debug> <log-prefix>
set -u
CFG=$1
LOG=${2:-/root/d1/build-clr-$CFG}
cd /root/d1/main
./build.sh -s clr -c $CFG -cmakeargs "-DCLR_CMAKE_ENABLE_LXR_GC=1" /p:RepositoryCommit=d1 /p:CrossgenOldValueWriteBarriers=true > $LOG.log 2>&1
echo "exit=$?" > $LOG.status
tail -5 $LOG.log
