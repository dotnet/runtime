#!/bin/bash
# Research-only: print the feature list the Java LXR core crate was built with.
cd /root/lxr/head/mmtk-openjdk/mmtk || exit 1
for d in target/release/.fingerprint/mmtk-*; do
  for j in "$d"/*.json; do
    [ -f "$j" ] || continue
    echo "== $j"
    python3 - "$j" <<'EOF'
import json, sys
try:
    data = json.load(open(sys.argv[1]))
    print("features:", data.get("features"))
except Exception as e:
    print("unreadable:", e)
EOF
  done
done 2>&1 | head -40
echo "--- vo_bit mentions in build env"
env | grep -i vo_bit
grep -rn "vo_bit" Cargo.toml | head -5
