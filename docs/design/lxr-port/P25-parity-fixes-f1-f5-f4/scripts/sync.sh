#!/bin/bash
# P25: copy the P25 scripts from the Windows worktree to /root/p25/scripts with LF line endings.
# usage: tr -d '\r' < sync.sh | bash -s <worktree-wsl-path>
W=$1
S=$W/docs/design/lxr-port/P25-parity-fixes-f1-f5-f4/scripts
mkdir -p /root/p25/scripts /root/p25/results
for f in "$S"/*; do
  [ -f "$f" ] || continue
  tr -d '\r' < "$f" > "/root/p25/scripts/$(basename "$f")"
done
chmod +x /root/p25/scripts/*.sh
ls /root/p25/scripts
