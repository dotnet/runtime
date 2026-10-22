#!/bin/bash
# Copy the P24 scripts from the Windows worktree to /root/p24/scripts, with LF line endings.
# usage (from the worktree, through WSL):  tr -d '\r' < scripts/sync.sh | bash -s <worktree-wsl-path>
W=$1
S=$W/docs/design/lxr-port/P24-slowness-investigation/scripts
mkdir -p /root/p24/scripts /root/p24/results
for f in "$S"/*; do
  n=$(basename "$f")
  if [ -d "$f" ]; then mkdir -p "/root/p24/scripts/$n"; for g in "$f"/*; do tr -d '\r' < "$g" > "/root/p24/scripts/$n/$(basename "$g")"; done; continue; fi
  tr -d '\r' < "$f" > "/root/p24/scripts/$n"
done
chmod +x /root/p24/scripts/*.sh
ls /root/p24/scripts
