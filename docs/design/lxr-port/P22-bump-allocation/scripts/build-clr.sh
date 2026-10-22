#!/bin/bash
# P22 (D2): build the Linux runtime (clr: runtime, JIT, CoreLib with ReadyToRun) with the LXR collector.
# usage: build-clr.sh <Release|Debug> [log-prefix]
. "$(dirname "$0")/env.sh"
CFG=$1
LOG=${2:-$D/build-clr-$CFG}
cd $T
./build.sh -s clr -c $CFG -cmakeargs "-DCLR_CMAKE_ENABLE_LXR_GC=1" /p:RepositoryCommit=d2 /p:CrossgenOldValueWriteBarriers=true > $LOG.log 2>&1
echo "exit=$?" > $LOG.status
