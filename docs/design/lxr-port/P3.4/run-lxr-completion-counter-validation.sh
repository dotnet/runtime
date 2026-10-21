#!/usr/bin/env bash
set -euo pipefail

repo_root="${1:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)}"
output="${2:-$repo_root/artifacts/P3.4/validation/linux-x64}"
if [[ -e "$output" ]]; then
    echo "Output directory already exists: $output" >&2
    exit 2
fi

mkdir -p "$output"
clang++ -std=c++17 -pthread -O2 -Wall -Wextra -Werror \
    -DLXR_ENABLE_LAZY_COMPLETION_COUNTER \
    "$repo_root/src/coreclr/gc/lxr_completion_counter.cpp" \
    "$repo_root/src/coreclr/gc/lxr_work_packet.cpp" \
    "$repo_root/src/coreclr/gc/lxr_work_scheduler.cpp" \
    "$repo_root/docs/design/lxr-port/P3.4/lxr-completion-counter-validation.cpp" \
    -o "$output/lxr-completion-counter-validation" \
    >"$output/build.log" 2>&1
"$output/lxr-completion-counter-validation" >"$output/run.log" 2>&1
line="$(grep -E '^PASS: [0-9]+/[0-9]+ P3\.4 validation checks$' "$output/run.log")"
passed="$(sed -E 's/^PASS: ([0-9]+)\/([0-9]+).*/\1/' <<<"$line")"
total="$(sed -E 's/^PASS: ([0-9]+)\/([0-9]+).*/\2/' <<<"$line")"
[[ "$passed" == "$total" ]]
printf '"platform","passed","total","result"\n"linux-x64","%s","%s","PASS"\n' \
    "$passed" "$total" >"$output/validation-summary.csv"
echo "$line"
