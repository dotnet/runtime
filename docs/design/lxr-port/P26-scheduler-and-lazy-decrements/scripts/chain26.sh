#!/bin/bash
# P26: the whole Linux test chain for one step, run in the foreground so that the caller can follow it.
# native selectors (Debug, Release), legacy suites, Debug collector build, 90 managed cells with the Debug collector.
# usage: chain26.sh <tag> <worktree-wsl-path>
TAG=$1; W=$2
R=/root/p25
tr -d '\r' < $W/docs/design/lxr-port/P17-pause-cost-and-reserve/native-linux.sh > $R/main/docs/design/lxr-port/P17-pause-cost-and-reserve/native-linux.sh
$R/scripts/chain-native.sh $TAG
(cd $R && ./build.sh $W Debug > $R/out/$TAG-build-debug.txt 2>&1)
$R/scripts/run-managed.sh $R/frames/p25-dbgcol-linux $R/out/managed-$TAG > $R/out/managed-$TAG.summary 2>&1
$R/scripts26/natsum.sh $TAG
cat $R/out/managed-$TAG.summary
