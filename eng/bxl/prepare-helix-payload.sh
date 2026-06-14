#!/usr/bin/env bash
set -euo pipefail

# Prepare BXL test outputs for Helix submission.
# Scans Out/Objects for compiled test DLLs, chunks them into work-item
# directories, and writes manifests for the batch runner.

usage() {
    echo "Usage: $0 --output <staging-dir> [--chunks <N>] [--objects-dir <path>]"
    echo ""
    echo "Options:"
    echo "  --output       Staging directory to create (required)"
    echo "  --chunks       Number of Helix work items to create (default: 30)"
    echo "  --objects-dir  BXL objects directory (default: Out/Objects)"
    exit 1
}

STAGING_DIR=""
CHUNKS=30
OBJECTS_DIR="Out/Objects"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --output) STAGING_DIR="$2"; shift 2 ;;
        --chunks) CHUNKS="$2"; shift 2 ;;
        --objects-dir) OBJECTS_DIR="$2"; shift 2 ;;
        -h|--help) usage ;;
        *) echo "Unknown option: $1" >&2; usage ;;
    esac
done

if [[ -z "$STAGING_DIR" ]]; then
    echo "ERROR: --output is required" >&2
    usage
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Find all test runner scripts — each corresponds to one compiled test
mapfile -t runners < <(find "$OBJECTS_DIR" -name '*.runner.sh' -type f | sort)
total=${#runners[@]}

if [[ $total -eq 0 ]]; then
    echo "ERROR: No test runners found in $OBJECTS_DIR" >&2
    exit 1
fi

echo "Found $total test runners in $OBJECTS_DIR"

# Adjust chunk count if we have fewer tests than chunks
if [[ $total -lt $CHUNKS ]]; then
    CHUNKS=$total
fi
tests_per_chunk=$(( (total + CHUNKS - 1) / CHUNKS ))
echo "Creating $CHUNKS work items (~$tests_per_chunk tests each)"

rm -rf "$STAGING_DIR"
mkdir -p "$STAGING_DIR"

# Track names for collision detection
declare -A name_counts

chunk_idx=0
chunk_count=0
chunk_dir=""

for i in "${!runners[@]}"; do
    runner="${runners[$i]}"

    # Start a new chunk when needed
    if [[ $((i % tests_per_chunk)) -eq 0 ]]; then
        chunk_dir="$STAGING_DIR/chunk-$(printf '%03d' $chunk_idx)"
        mkdir -p "$chunk_dir/tests"
        cp "$SCRIPT_DIR/bxl-helix-runner.sh" "$chunk_dir/run-tests.sh"
        chmod +x "$chunk_dir/run-tests.sh"
        chunk_idx=$((chunk_idx + 1))
    fi

    # Extract DLL filename from runner script
    # Format: exec ".../corerun" "$(dirname "$0")/<name>.dll" "$@"
    dll_name=$(grep -oP '\$\(dirname "\$0"\)/\K[^"]+' "$runner" 2>/dev/null || true)
    if [[ -z "$dll_name" ]]; then
        echo "WARNING: Could not extract DLL from $runner, skipping" >&2
        continue
    fi

    runner_dir=$(dirname "$runner")

    # Extract the DLL's absolute source path from the runner script metadata.
    # Format: # dll-source: /absolute/path/to/<name>.dll
    dll_path=$(grep -oP '^# dll-source: \K.*' "$runner" 2>/dev/null || true)

    if [[ -z "$dll_path" || ! -f "$dll_path" ]]; then
        echo "WARNING: DLL not found: ${dll_path:-<no dll-source in $runner>}, skipping" >&2
        continue
    fi

    # Derive a unique subdirectory name
    base_name=$(basename "$dll_name" .dll)
    count="${name_counts[$base_name]:-0}"
    name_counts[$base_name]=$((count + 1))

    if [[ $count -gt 0 ]]; then
        subdir="${base_name}_${count}"
    else
        subdir="$base_name"
    fi

    target_dir="$chunk_dir/tests/$subdir"
    mkdir -p "$target_dir"

    # Hard-link (fast, same filesystem) or fall back to copy
    ln "$dll_path" "$target_dir/$dll_name" 2>/dev/null || cp "$dll_path" "$target_dir/$dll_name"

    runner_name=$(basename "$runner")
    cp "$runner" "$target_dir/$runner_name"
    chmod +x "$target_dir/$runner_name"

    # Copy runtime dependencies declared by the generated runner metadata.
    # Format: # runtime-source: /absolute/path/to/<dependency>
    while IFS= read -r runtime_path; do
        [[ -n "$runtime_path" ]] || continue

        if [[ ! -f "$runtime_path" ]]; then
            echo "WARNING: Runtime dependency not found: $runtime_path" >&2
            continue
        fi

        runtime_name=$(basename "$runtime_path")
        ln "$runtime_path" "$target_dir/$runtime_name" 2>/dev/null || cp "$runtime_path" "$target_dir/$runtime_name"
    done < <(grep -oP '^# runtime-source: \K.*' "$runner" 2>/dev/null || true)

    echo "tests/$subdir/$runner_name" >> "$chunk_dir/manifest.txt"
    chunk_count=$((chunk_count + 1))
done

echo ""
echo "Staged $chunk_count tests into $chunk_idx chunks under $STAGING_DIR"
echo "Total staging size: $(du -sh "$STAGING_DIR" | cut -f1)"
