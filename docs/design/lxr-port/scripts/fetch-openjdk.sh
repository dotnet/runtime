#!/bin/bash
# P0.1 - fetch OpenJDK sources for both revisions.
# Both revisions use the SAME upstream repo (mmtk/openjdk.git), different commits:
#   PLDI: f817e9d00b2850221bb9443443a123e38e81a129  (branch jdk-11.0.11+6-mmtk)
#   head: 7caf8f7d19b19a9fd53be6f909db805246790807  (from mmtk/Cargo.toml metadata)
# Clone once into a cache, then create two working clones from it.
set -euo pipefail
cd /root

CACHE=/root/lxr/openjdk-cache.git
PLDI_REV=f817e9d00b2850221bb9443443a123e38e81a129
HEAD_REV=7caf8f7d19b19a9fd53be6f909db805246790807

if [ ! -d "$CACHE" ]; then
  echo "=== cloning mmtk/openjdk.git into cache (this is large) ==="
  git clone --bare --quiet https://github.com/mmtk/openjdk.git "$CACHE"
else
  echo "=== cache already present ==="
fi

echo "=== verifying both revisions exist in cache ==="
git -C "$CACHE" cat-file -t "$PLDI_REV"
git -C "$CACHE" cat-file -t "$HEAD_REV"

for pair in "pldi:$PLDI_REV" "head:$HEAD_REV"; do
  rev=${pair%%:*}; sha=${pair##*:}
  dst=/root/lxr/$rev/mmtk-openjdk/repos/openjdk
  echo "=== materialising $rev openjdk @ ${sha:0:8} ==="
  rm -rf "$dst"
  mkdir -p "$(dirname "$dst")"
  git clone --quiet --no-checkout "$CACHE" "$dst"
  git -C "$dst" checkout --quiet "$sha"
  printf "  %s -> %s  %s\n" "$rev" \
    "$(git -C "$dst" rev-parse --short HEAD)" \
    "$(git -C "$dst" log -1 --format=%s | head -c 60)"
done

echo "=== disk ==="
df -h / | tail -1
du -sh /root/lxr/pldi/mmtk-openjdk/repos/openjdk /root/lxr/head/mmtk-openjdk/repos/openjdk
