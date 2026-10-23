#!/bin/bash
# P26: copy the P26 scripts from the Windows worktree to /root/p25/scripts26 with LF line endings.
# (The P25 scripts, build.sh, one.sh and so on, are reused unchanged from /root/p25/scripts.)
# usage: tr -d '\r' < sync26.sh | bash -s <worktree-wsl-path>
W=$1
S=$W/docs/design/lxr-port/P26-scheduler-and-lazy-decrements/scripts
mkdir -p /root/p25/scripts26
for f in "$S"/*; do
  [ -f "$f" ] || continue
  tr -d '\r' < "$f" > "/root/p25/scripts26/$(basename "$f")"
done
chmod +x /root/p25/scripts26/*.sh
ls /root/p25/scripts26 | tr '\n' ' '
echo
