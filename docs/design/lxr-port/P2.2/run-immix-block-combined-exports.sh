#!/usr/bin/env bash
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

set -euo pipefail

if [[ $# -ne 2 ]]; then
  echo "usage: $0 <combined-feature-libcoreclr.so> <output-root>" >&2
  exit 2
fi

library="$(realpath "$1")"
output_root="$2"
[[ -f "$library" ]] || {
  echo "Combined-feature CoreCLR is missing: '$library'." >&2
  exit 3
}
file "$library" | grep -Eq 'ELF 64-bit.*x86-64' || {
  echo "Combined-feature CoreCLR is not x86-64: '$library'." >&2
  exit 3
}

mkdir -p "$output_root"
symbols=(
  GC_ImmixBlockStateTest_Run
  GC_WriteBarrierTest_Reset
  GC_AllocationNotificationTest_Reset
)
printf 'symbol,result\n' > "$output_root/combined-export-summary.csv"
for symbol in "${symbols[@]}"; do
  nm -D --defined-only "$library" | grep -Eq "[[:space:]]${symbol}(@@[^[:space:]]+)?$" || {
    echo "Combined-feature export is missing: '$symbol'." >&2
    exit 4
  }
  printf '%s,PASS\n' "$symbol" >> "$output_root/combined-export-summary.csv"
done

echo "PASS: ${#symbols[@]} combined-feature exports"
echo "Output: $output_root"
