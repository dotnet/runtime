#!/bin/bash
# P25: the whole Linux test chain for one fix: native selectors (Debug, Release), legacy suites, Debug collector build and managed cells.
# usage: chain-all.sh <tag> <worktree-wsl-path>
TAG=$1; W=$2
R=/root/p25
rm -f $R/out/$TAG.done
$R/scripts/chain-native.sh $TAG
(cd $R && ./build.sh $W Debug > $R/out/$TAG-build-debug.txt 2>&1)
$R/scripts/run-managed.sh $R/frames/p25-dbgcol-linux $R/out/managed-$TAG > $R/out/managed-$TAG.summary 2>&1
echo done > $R/out/$TAG-all.done
