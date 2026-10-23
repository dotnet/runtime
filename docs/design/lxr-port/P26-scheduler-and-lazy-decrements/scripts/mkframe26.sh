#!/bin/bash
# P26: build a collector frame of an older commit without leaving the working tree. Run from the Windows worktree path.
# usage: mkframe26.sh <worktree-wsl-path> <commit> <frame-name>
W=$1; C=$2; N=$3
cd $W
git checkout $C -- src/coreclr/gc
(cd /root/p25 && ./build.sh $W Release $N 2>&1 | tail -2)
git checkout HEAD -- src/coreclr/gc
git status --short | grep -v '^??' | head
