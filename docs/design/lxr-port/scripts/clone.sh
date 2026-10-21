#!/bin/bash
# P0.1 - clone both LXR reference revisions into WSL ext4
set -euo pipefail
cd /root

REF=/mnt/c/github/lxr-reference
mkdir -p /root/lxr/pldi /root/lxr/head
git config --global --add safe.directory '*'

echo "=== cloning pldi/mmtk-core @ 4d4e516c ==="
rm -rf /root/lxr/pldi/mmtk-core
git clone -q --no-checkout "$REF/mmtk-core" /root/lxr/pldi/mmtk-core
git -C /root/lxr/pldi/mmtk-core checkout -q 4d4e516c

echo "=== cloning pldi/mmtk-openjdk @ abbdd1d ==="
rm -rf /root/lxr/pldi/mmtk-openjdk
git clone -q --no-checkout "$REF/mmtk-openjdk" /root/lxr/pldi/mmtk-openjdk
git -C /root/lxr/pldi/mmtk-openjdk checkout -q abbdd1d

echo "=== cloning head/mmtk-core @ 9625c174 ==="
rm -rf /root/lxr/head/mmtk-core
git clone -q --no-checkout "$REF/mmtk-core" /root/lxr/head/mmtk-core
git -C /root/lxr/head/mmtk-core checkout -q 9625c174

echo "=== cloning head/mmtk-openjdk @ 0682434 ==="
rm -rf /root/lxr/head/mmtk-openjdk
git clone -q --no-checkout "$REF/mmtk-openjdk" /root/lxr/head/mmtk-openjdk
git -C /root/lxr/head/mmtk-openjdk checkout -q 0682434

echo "=== RESULT ==="
for d in pldi/mmtk-core pldi/mmtk-openjdk head/mmtk-core head/mmtk-openjdk; do
  printf "%-22s %s  %s\n" "$d" \
    "$(git -C "/root/lxr/$d" rev-parse --short HEAD)" \
    "$(git -C "/root/lxr/$d" log -1 --format=%s)"
done

echo "=== reference checkout must be UNCHANGED ==="
for d in mmtk-core mmtk-openjdk; do
  printf "%-14s HEAD=%s  branch=%s  dirty=[%s]\n" "$d" \
    "$(git -C "$REF/$d" rev-parse --short HEAD)" \
    "$(git -C "$REF/$d" rev-parse --abbrev-ref HEAD)" \
    "$(git -C "$REF/$d" status --porcelain | head -c 200)"
done
