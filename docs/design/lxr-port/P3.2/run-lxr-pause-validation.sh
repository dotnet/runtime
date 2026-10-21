#!/usr/bin/env bash
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

set -euo pipefail

script_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="${1:-$(cd "$script_root/../../../.." && pwd)}"
output="${2:-$repo_root/artifacts/P3.2/validation/linux-x64}"

if [[ -e "$output" ]]; then
    echo "Output directory already exists: $output" >&2
    exit 2
fi

mkdir -p "$output"
g++ -std=c++17 -pthread -Wall -Wextra -Werror -O2 \
    "$repo_root/src/coreclr/gc/lxr_work_packet.cpp" \
    "$repo_root/src/coreclr/gc/lxr_work_scheduler.cpp" \
    "$repo_root/src/coreclr/gc/lxr_pause_schedule.cpp" \
    "$repo_root/src/coreclr/gc/lxr_pause_coordinator.cpp" \
    "$script_root/lxr-pause-validation.cpp" \
    -o "$output/lxr-pause-validation" \
    >"$output/build.log" 2>&1

"$output/lxr-pause-validation" >"$output/validation.log" 2>&1
summary="$(grep -E '^PASS: [0-9]+/[0-9]+ P3\.2 pause checks$' "$output/validation.log")"
passed="$(sed -E 's/^PASS: ([0-9]+)\/([0-9]+).*/\1/' <<<"$summary")"
total="$(sed -E 's/^PASS: ([0-9]+)\/([0-9]+).*/\2/' <<<"$summary")"
{
    echo '"platform","passed","total","exit_code","result"'
    echo "\"linux-x64\",\"$passed\",\"$total\",\"0\",\"PASS\""
} >"$output/validation-summary.csv"
echo "$summary"
