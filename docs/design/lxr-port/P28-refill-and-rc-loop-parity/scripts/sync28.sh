#!/bin/bash
# P28: copy the P28 scripts from the Windows worktree to /root/p28/scripts with LF line endings.
# (The P24, P25 and P26 scripts in /root/p24/scripts, /root/p25/scripts and /root/p25/scripts26 are reused unchanged.)
# usage: tr -d '\r' < sync28.sh | bash -s <worktree-wsl-path>
W=$1
S=$W/docs/design/lxr-port/P28-refill-and-rc-loop-parity/scripts
mkdir -p /root/p28/scripts /root/p28/results /root/p28/perf
for f in "$S"/*; do
  [ -f "$f" ] || continue
  tr -d '\r' < "$f" > "/root/p28/scripts/$(basename "$f")"
done
chmod +x /root/p28/scripts/*.sh
ls /root/p28/scripts | tr '\n' ' '
echo
