#!/bin/bash
# P25: copy the collector sources and the lxr-port documents (with the test sources) from the worktree to /root/p25/main, LF line endings.
# usage: sync-src.sh <worktree-wsl-path>
set -eu
W=$1
R=/root/p25
rsync -a --delete $W/src/coreclr/gc/ $R/main/src/coreclr/gc/
mkdir -p $R/main/docs/design/lxr-port
rsync -a --delete $W/docs/design/lxr-port/ $R/main/docs/design/lxr-port/ --exclude='*/results' --exclude='*/benchmark-results' --exclude='*.html' --exclude='*.png'
find $R/main/src/coreclr/gc $R/main/docs/design/lxr-port -type f \( -name '*.h' -o -name '*.cpp' -o -name '*.sh' -o -name '*.py' -o -name 'CMakeLists.txt' -o -name '*.cmake' -o -name '*.inc' \) -exec sed -i 's/\r$//' {} +
echo synced
