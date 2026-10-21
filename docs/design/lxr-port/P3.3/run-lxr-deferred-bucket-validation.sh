#!/usr/bin/env bash
set -euo pipefail

repo_root="${1:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)}"
output="${2:-$repo_root/artifacts/P3.3/validation/linux-x64}"
script_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [[ -e "$output" ]]; then
    echo "Output directory already exists: $output" >&2
    exit 1
fi

mkdir -p "$output"
c++ -std=c++17 -O2 -pthread \
    "$repo_root/src/coreclr/gc/lxr_work_packet.cpp" \
    "$repo_root/src/coreclr/gc/lxr_work_scheduler.cpp" \
    "$script_root/lxr-deferred-bucket-validation.cpp" \
    -o "$output/lxr-deferred-bucket-validation" \
    >"$output/build.log" 2>&1

"$output/lxr-deferred-bucket-validation" >"$output/run.log" 2>&1
summary="$(grep -E '^PASS: [0-9]+/[0-9]+ P3\.3 validation checks$' "$output/run.log")"
passed="$(printf '%s' "$summary" | sed -E 's/^PASS: ([0-9]+)\/([0-9]+).*/\1/')"
total="$(printf '%s' "$summary" | sed -E 's/^PASS: ([0-9]+)\/([0-9]+).*/\2/')"
if [[ "$passed" != "$total" ]]; then
    echo "P3.3 Linux validation success count is incomplete." >&2
    exit 1
fi

printf '"platform","passed","total","result"\n"linux-x64","%s","%s","PASS"\n' \
    "$passed" "$total" >"$output/validation-summary.csv"
printf '%s\n' "$summary"
