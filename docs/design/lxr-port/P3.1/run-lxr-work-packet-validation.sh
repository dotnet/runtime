#!/usr/bin/env bash
set -euo pipefail

script_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="${1:-$(cd "$script_root/../../../.." && pwd)}"
output_root="${2:-$repo_root/artifacts/P3.1/validation/linux-x64}"

if [[ -e "$output_root" ]]; then
    echo "Output directory already exists: $output_root" >&2
    exit 2
fi

mkdir -p "$output_root"

c++ -std=c++17 -O2 -pthread \
    -I"$repo_root/src/coreclr/gc" \
    "$repo_root/src/coreclr/gc/lxr_work_packet.cpp" \
    "$repo_root/src/coreclr/gc/lxr_work_scheduler.cpp" \
    "$script_root/lxr-work-packet-validation.cpp" \
    -o "$output_root/lxr-work-packet-validation" \
    >"$output_root/build.log" 2>&1

"$output_root/lxr-work-packet-validation" >"$output_root/run.log" 2>&1
grep -E '^PASS: [0-9]+/[0-9]+ P3\.1 validation checks$' "$output_root/run.log"

checks="$(sed -n 's/^PASS: \([0-9][0-9]*\)\/\([0-9][0-9]*\) P3\.1 validation checks$/\1,\2/p' "$output_root/run.log")"
if [[ -z "$checks" ]]; then
    echo "Validation count was not found." >&2
    exit 3
fi

printf 'platform,passed,total,result\nlinux-x64,%s,PASS\n' "$checks" \
    >"$output_root/validation-summary.csv"
