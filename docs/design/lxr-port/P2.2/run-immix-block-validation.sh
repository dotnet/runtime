#!/usr/bin/env bash
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

set -euo pipefail

script_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="${1:-$(cd "$script_root/../../../.." && pwd)}"
output_root="${2:-$repository_root/artifacts/P2.2/validation/linux-$(date -u +%Y%m%dT%H%M%SZ)-$$}"
gc_root="$repository_root/src/coreclr/gc"
native_root="$repository_root/src/native"

mkdir -p "$output_root"

command=(
  g++
  -std=c++17
  -O2
  -Wall
  -Wextra
  -Werror
  -Wno-invalid-offsetof
  -Wno-strict-aliasing
  -Wno-unused-parameter
  -pthread
  -DBUILD_AS_STANDALONE
  -DTARGET_UNIX
  -DHOST_UNIX
  -DTARGET_LINUX
  -DHOST_LINUX
  -DTARGET_AMD64
  -DHOST_AMD64
  -DTARGET_64BIT
  -DHOST_64BIT
  -I"$gc_root"
  -I"$gc_root/env"
  -I"$native_root"
  -I"$native_root/inc"
  "$script_root/immix-block-validation.cpp"
  "$script_root/../P2.1/side-metadata-test-platform.cpp"
  "$gc_root/side_metadata.cpp"
  "$gc_root/immix_block.cpp"
  -o "$output_root/immix-block-validation"
)

printf '%q ' "${command[@]}" > "$output_root/command.txt"
printf '\n' >> "$output_root/command.txt"
"${command[@]}" > "$output_root/build.log" 2>&1
"$output_root/immix-block-validation" > "$output_root/run.log" 2>&1
result="$(sed -nE 's/^([0-9]+)\/([0-9]+) immix block checks passed$/\1 \2/p' "$output_root/run.log")"
read -r passed total <<< "$result"
[[ -n "${passed:-}" && "$passed" == "$total" && "$total" -gt 0 ]] || {
  cat "$output_root/run.log"
  exit 1
}

printf 'platform,passed,total,result\n' > "$output_root/validation-summary.csv"
sed -nE 's/^([0-9]+)\/([0-9]+) immix block checks passed$/linux-x64,\1,\2,PASS/p' \
  "$output_root/run.log" >> "$output_root/validation-summary.csv"
printf 'PASS: Linux x64 Immix block validation\n'
printf 'Output: %s\n' "$output_root"
