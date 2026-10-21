#!/usr/bin/env bash
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

set -euo pipefail

if [[ $# -lt 2 || $# -gt 5 ]]; then
  echo "usage: $0 <combined-feature-libcoreclr.so> <output-root> [default-libcoreclr.so default-libclrgc.so repository-root]" >&2
  exit 2
fi

library="$(realpath "$1")"
output_root="$2"
default_library="${3:-}"
default_standalone="${4:-}"
repository_root="${5:-}"

require_elf_x86_64() {
  local path="$1"
  local label="$2"
  [[ -f "$path" ]] || {
    echo "$label CoreCLR is missing: '$path'." >&2
    exit 3
  }
  file "$path" | grep -Eq 'ELF 64-bit.*x86-64' || {
    echo "$label CoreCLR is not x86-64: '$path'." >&2
    exit 3
  }
}

write_dynamic_symbols() {
  local path="$1"
  local output="$2"
  nm -D --defined-only "$path" > "$output" || {
    echo "Unable to read dynamic symbols from '$path'." >&2
    exit 3
  }
}

elf_build_id() {
  local path="$1"
  local id
  id="$(
    readelf -n "$path" 2>/dev/null |
      sed -nE 's/^[[:space:]]*Build ID: ([0-9a-f]+)$/\1/p'
  )"
  [[ "$id" =~ ^[0-9a-f]+$ ]] || {
    echo "ELF build ID is missing or malformed: '$path'." >&2
    return 3
  }
  printf '%s\n' "$id"
}

require_no_defined_symbol() {
  local path="$1"
  local symbol="$2"
  local label="$3"
  local output="$output_root/${label}-symbols.txt"
  nm -C --defined-only "$path" > "$output" || {
    echo "Unable to read symbols from '$path'." >&2
    exit 3
  }
  set +e
  grep -E "$symbol" "$output" > /dev/null
  local status=$?
  set -e
  if [[ $status -eq 0 ]]; then
    echo "$label unexpectedly links a P2.3 product object." >&2
    exit 6
  elif [[ $status -ne 1 ]]; then
    echo "Unable to search symbols from '$path'." >&2
    exit 3
  fi
}

require_elf_x86_64 "$library" "Combined-feature"

mkdir -p "$output_root"
symbols=(
  GC_ReferenceCountTest_Run
  GC_ImmixBlockStateTest_Run
  GC_WriteBarrierTest_Reset
  GC_AllocationNotificationTest_Reset
)
printf 'symbol,result\n' > "$output_root/combined-export-summary.csv"
combined_dynamic_symbols="$output_root/combined-dynamic-symbols.txt"
write_dynamic_symbols "$library" "$combined_dynamic_symbols"
for symbol in "${symbols[@]}"; do
  grep -E "[[:space:]]${symbol}(@@[^[:space:]]+)?$" \
    "$combined_dynamic_symbols" > /dev/null || {
    echo "Combined-feature export is missing: '$symbol'." >&2
    exit 4
  }
  printf '%s,PASS\n' "$symbol" >> "$output_root/combined-export-summary.csv"
done

printf 'property,observed,result\n' > "$output_root/default-path-summary.csv"
if [[ -n "$default_library" ]]; then
  [[ -n "$default_standalone" && -n "$repository_root" ]] || {
    echo "Default-path proof requires default CoreCLR, standalone GC, and repository root." >&2
    exit 2
  }
  default_library="$(realpath "$default_library")"
  default_standalone="$(realpath "$default_standalone")"
  repository_root="$(realpath "$repository_root")"
  default_library_symbols="${default_library}.dbg"
  default_standalone_symbols="${default_standalone}.dbg"
  require_elf_x86_64 "$default_library" "Default"
  require_elf_x86_64 "$default_standalone" "Default standalone"
  require_elf_x86_64 "$default_library_symbols" "Default CoreCLR symbols"
  require_elf_x86_64 "$default_standalone_symbols" "Default standalone symbols"
  if ! default_library_id="$(elf_build_id "$default_library")"; then exit 3; fi
  if ! default_library_symbols_id="$(elf_build_id "$default_library_symbols")"; then exit 3; fi
  if ! default_standalone_id="$(elf_build_id "$default_standalone")"; then exit 3; fi
  if ! default_standalone_symbols_id="$(elf_build_id "$default_standalone_symbols")"; then exit 3; fi
  [[ "$default_library_id" == "$default_library_symbols_id" ]] || {
    echo "Default CoreCLR symbol sidecar does not match its binary." >&2
    exit 3
  }
  [[ "$default_standalone_id" == "$default_standalone_symbols_id" ]] || {
    echo "Default standalone symbol sidecar does not match its binary." >&2
    exit 3
  }
  printf 'default-coreclr-build-id,%s,PASS\n' "$default_library_id" \
    >> "$output_root/default-path-summary.csv"
  printf 'default-clrgc-build-id,%s,PASS\n' "$default_standalone_id" \
    >> "$output_root/default-path-summary.csv"
  if [[ "$default_library" == "$library" ]]; then
    echo "The default and combined-feature CoreCLR must be distinct binaries." >&2
    exit 5
  fi
  grep -A1 'OPTION(CLR_CMAKE_ENABLE_REFERENCE_COUNT_TEST' \
    "$repository_root/src/coreclr/CMakeLists.txt" |
    grep -q 'OFF)' || {
      echo "The reference-count validation option is not default-off." >&2
      exit 6
    }
  printf 'cmake-default,OFF,PASS\n' >> "$output_root/default-path-summary.csv"

  set +e
  call_sites="$(
    grep -R -E 'ReferenceCountTable|GC_ReferenceCountTest_Run' \
      "$repository_root/src/coreclr/gc" \
      --include='*.cpp' \
      --exclude='reference_count.cpp' \
      --exclude='referencecounttest.cpp'
  )"
  call_site_status=$?
  set -e
  if [[ $call_site_status -eq 0 ]]; then
    echo "A normal GC source contains a P2.3 call or branch." >&2
    exit 6
  elif [[ $call_site_status -ne 1 ]]; then
    echo "Unable to scan normal GC sources for P2.3 call sites." >&2
    exit 3
  fi
  [[ -z "$call_sites" ]] || {
    echo "A failed call-site scan returned unexpected output." >&2
    exit 3
  }
  printf 'normal-gc-call-sites,0,PASS\n' >> "$output_root/default-path-summary.csv"

  default_library_dynamic_symbols="$output_root/default-coreclr-dynamic-symbols.txt"
  write_dynamic_symbols "$default_library" "$default_library_dynamic_symbols"
  set +e
  grep -E '[[:space:]]GC_ReferenceCountTest_Run(@@[^[:space:]]+)?$' \
    "$default_library_dynamic_symbols" > /dev/null
  default_library_export_status=$?
  set -e
  if [[ $default_library_export_status -eq 0 ]]; then
    echo "Default CoreCLR unexpectedly exports GC_ReferenceCountTest_Run." >&2
    exit 6
  elif [[ $default_library_export_status -ne 1 ]]; then
    echo "Unable to search default CoreCLR dynamic symbols." >&2
    exit 3
  fi
  printf 'default-coreclr-export,0,PASS\n' >> "$output_root/default-path-summary.csv"
  default_standalone_dynamic_symbols="$output_root/default-clrgc-dynamic-symbols.txt"
  write_dynamic_symbols "$default_standalone" "$default_standalone_dynamic_symbols"
  set +e
  grep -E '[[:space:]]GC_ReferenceCountTest_Run(@@[^[:space:]]+)?$' \
    "$default_standalone_dynamic_symbols" > /dev/null
  default_standalone_export_status=$?
  set -e
  if [[ $default_standalone_export_status -eq 0 ]]; then
    echo "Default standalone GC unexpectedly exports GC_ReferenceCountTest_Run." >&2
    exit 6
  elif [[ $default_standalone_export_status -ne 1 ]]; then
    echo "Unable to search default standalone dynamic symbols." >&2
    exit 3
  fi
  printf 'default-clrgc-export,0,PASS\n' >> "$output_root/default-path-summary.csv"

  require_no_defined_symbol \
    "$default_library_symbols" \
    'ReferenceCountTable::' \
    'Default CoreCLR'
  printf 'default-coreclr-object,0,PASS\n' >> "$output_root/default-path-summary.csv"
  require_no_defined_symbol \
    "$default_standalone_symbols" \
    'ReferenceCountTable::' \
    'Default standalone GC'
  printf 'default-clrgc-object,0,PASS\n' >> "$output_root/default-path-summary.csv"
else
  printf 'default-binary,not-supplied,SKIP\n' \
    >> "$output_root/default-path-summary.csv"
fi

echo "PASS: ${#symbols[@]} combined-feature exports"
echo "Output: $output_root"
