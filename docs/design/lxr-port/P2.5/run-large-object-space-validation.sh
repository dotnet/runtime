#!/usr/bin/env bash
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

set -euo pipefail

script_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="${1:-$(cd "$script_root/../../../.." && pwd)}"
output_root="${2:-$repository_root/artifacts/P2.5/validation/linux-$(date -u +%Y%m%dT%H%M%SZ)-$$}"
gc_root="$repository_root/src/coreclr/gc"
native_root="$repository_root/src/native"

mkdir -p "$output_root"
command=(
  g++ -std=c++17 -O2 -Wall -Wextra -Werror
  -Wno-invalid-offsetof -Wno-strict-aliasing -Wno-unused-parameter -pthread
  -DBUILD_AS_STANDALONE -DLXR_SIDE_METADATA_TEST_PLATFORM
  -DTARGET_UNIX -DHOST_UNIX -DTARGET_LINUX -DHOST_LINUX
  -DTARGET_AMD64 -DHOST_AMD64 -DTARGET_64BIT -DHOST_64BIT
  -I"$gc_root" -I"$gc_root/env" -I"$native_root" -I"$native_root/inc"
  "$script_root/large-object-space-validation.cpp"
  "$script_root/large-object-space-test-platform.cpp"
  "$gc_root/side_metadata.cpp"
  "$gc_root/immix_block.cpp"
  "$gc_root/large_object_space.cpp"
  -o "$output_root/large-object-space-validation"
)

printf '%q ' "${command[@]}" > "$output_root/command.txt"
printf '\n' >> "$output_root/command.txt"
"${command[@]}" > "$output_root/build.log" 2>&1
"$output_root/large-object-space-validation" > "$output_root/run.log" 2>&1
result="$(sed -nE 's/^([0-9]+)\/([0-9]+) large object space checks passed$/\1 \2/p' "$output_root/run.log")"
read -r passed total <<< "$result"
[[ -n "${passed:-}" && "$passed" == "$total" && "$total" -gt 0 ]]
printf 'platform,passed,total,result\nlinux-x64,%s,%s,PASS\n' "$passed" "$total" \
  > "$output_root/validation-summary.csv"
printf 'PASS: Linux x64 large object space validation\n'
