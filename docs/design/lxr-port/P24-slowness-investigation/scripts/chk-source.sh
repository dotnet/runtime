#!/bin/bash
# Check that the collector sources in the worktree equal the sources that built the frame (/root/d2/main).
# usage: chk-source.sh <worktree-gc-dir>
R=$1
cd "$R" || exit 1
for f in lxr_*.cpp lxr_*.h immix_*.cpp immix_*.h side_metadata.* work_*; do
  [ -f "$f" ] || continue
  a=$(tr -d '\r' < "$f" | md5sum | cut -c1-8)
  b=$(tr -d '\r' < "/root/d2/main/src/coreclr/gc/$f" | md5sum | cut -c1-8)
  [ "$a" = "$b" ] || echo "DIFF $f"
done
echo checked
uptime
