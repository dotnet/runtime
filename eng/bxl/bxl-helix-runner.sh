#!/usr/bin/env bash
set -euo pipefail

# Batch runner for BXL-compiled CoreCLR tests on Helix.
# Reads a manifest of test DLLs and runs each via corerun.
# Exit code 100 from corerun = pass, anything else = fail.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# On Helix, Core_Root is the correlation payload
CORE_ROOT="${CORE_ROOT:-${HELIX_CORRELATION_PAYLOAD:-}}"
TIMEOUT_SECONDS="${BXL_TEST_TIMEOUT:-120}"
MANIFEST="${SCRIPT_DIR}/manifest.txt"

if [[ -z "$CORE_ROOT" ]]; then
    echo "ERROR: CORE_ROOT or HELIX_CORRELATION_PAYLOAD must be set" >&2
    exit 1
fi

if [[ ! -f "$CORE_ROOT/corerun" ]]; then
    echo "ERROR: corerun not found at $CORE_ROOT/corerun" >&2
    exit 1
fi

if [[ ! -f "$MANIFEST" ]]; then
    echo "ERROR: manifest.txt not found at $MANIFEST" >&2
    exit 1
fi

export CORE_ROOT

total=0
passed=0
failed=0
failed_tests=()

while IFS= read -r test_relative || [[ -n "$test_relative" ]]; do
    [[ -z "$test_relative" ]] && continue
    [[ "$test_relative" == \#* ]] && continue

    test_path="${SCRIPT_DIR}/${test_relative}"
    test_name=$(basename "$(dirname "$test_relative")")
    total=$((total + 1))

    if [[ ! -f "$test_path" ]]; then
        echo "SKIP: $test_name — test runner not found: $test_path"
        continue
    fi

    set +e
    timeout --foreground --kill-after=10 "$TIMEOUT_SECONDS" bash "$test_path" 2>&1
    rc=$?
    set -e

    if [[ $rc -eq 100 ]]; then
        echo "PASS: $test_name"
        passed=$((passed + 1))
    elif [[ $rc -eq 124 || $rc -eq 137 ]]; then
        echo "FAIL: $test_name — timed out after ${TIMEOUT_SECONDS}s"
        failed=$((failed + 1))
        failed_tests+=("$test_name")
    else
        echo "FAIL: $test_name — exit code $rc (expected 100)"
        failed=$((failed + 1))
        failed_tests+=("$test_name")
    fi
done < "$MANIFEST"

echo ""
echo "=========================================="
echo "Results: $passed/$total passed, $failed failed"
echo "=========================================="

if [[ $failed -gt 0 ]]; then
    echo "Failed tests:"
    for t in "${failed_tests[@]}"; do
        echo "  - $t"
    done
    exit 1
fi

exit 0
