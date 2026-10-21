#!/bin/bash
# Run the full matrix in the foreground. Start it from an attached WSL session so the VM keeps running it.
set -eu
cd /root/gcbench
OUT=results/matrix-$(date +%Y%m%d-%H%M%S)
mkdir -p "$OUT"
cp sizes.json "$OUT/sizes.json"
echo "output=$OUT"
exec python3 -u run-matrix.py run --factors 2,4,8 --reps 3 --timeout 400 \
  --collectors java-lxr,java-g1,java-parallel,java-serial,java-zgc,net-lxr-nonmoving,net-lxr-nursery,net-lxr-mature,net-wks,net-server \
  --output "$OUT/runs.jsonl"
