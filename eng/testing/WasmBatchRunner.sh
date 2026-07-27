#!/usr/bin/env bash

if [[ -z "$HELIX_WORKITEM_UPLOAD_ROOT" ]]; then
    ORIGINAL_UPLOAD_ROOT="$PWD/test-results"
else
    ORIGINAL_UPLOAD_ROOT="$HELIX_WORKITEM_UPLOAD_ROOT"
fi

BATCH_DIR="$PWD"
SUITE_COUNT=0
FAIL_COUNT=0
SUITE_NAMES=()
SUITE_EXIT_CODES=()
SUITE_DURATIONS=()

echo "=== WasmBatchRunner ==="
echo "BATCH_DIR=$BATCH_DIR"
echo "ORIGINAL_UPLOAD_ROOT=$ORIGINAL_UPLOAD_ROOT"

for zipFile in "$BATCH_DIR"/*.zip; do
    if [[ ! -f "$zipFile" ]]; then
        echo "No .zip files found in $BATCH_DIR"
        exit 1
    fi

    suiteName=$(basename "$zipFile" .zip)
    suiteDir="$BATCH_DIR/$suiteName"

    echo ""
    echo "========================= BEGIN $suiteName ============================="

    mkdir -p "$suiteDir"
    unzip -q -o "$zipFile" -d "$suiteDir"
    unzipExitCode=$?
    if [[ $unzipExitCode -ne 0 ]]; then
        echo "ERROR: Failed to extract $zipFile (exit code: $unzipExitCode)"
        FAIL_COUNT=$((FAIL_COUNT + 1))
        SUITE_NAMES+=("$suiteName")
        SUITE_EXIT_CODES+=("$unzipExitCode")
        SUITE_DURATIONS+=("0")
        SUITE_COUNT=$((SUITE_COUNT + 1))
        rm -rf "$suiteDir"
        continue
    fi

    export HELIX_WORKITEM_UPLOAD_ROOT="$ORIGINAL_UPLOAD_ROOT/$suiteName"
    mkdir -p "$HELIX_WORKITEM_UPLOAD_ROOT"

    pushd "$suiteDir" >/dev/null

    chmod +x RunTests.sh

    startTime=$(date +%s)
    ./RunTests.sh "$@"
    suiteExitCode=$?
    endTime=$(date +%s)

    popd >/dev/null

    rm -rf "$suiteDir"

    duration=$((endTime - startTime))

    SUITE_NAMES+=("$suiteName")
    SUITE_EXIT_CODES+=("$suiteExitCode")
    SUITE_DURATIONS+=("$duration")
    SUITE_COUNT=$((SUITE_COUNT + 1))

    if [[ $suiteExitCode -ne 0 ]]; then
        FAIL_COUNT=$((FAIL_COUNT + 1))
        echo "----- FAIL $suiteName — exit code $suiteExitCode — ${duration}s -----"
    else
        echo "----- PASS $suiteName — ${duration}s -----"
    fi

    echo "========================= END $suiteName ==============================="
done

# Restore so Helix post-commands write artifacts to the expected root
export HELIX_WORKITEM_UPLOAD_ROOT="$ORIGINAL_UPLOAD_ROOT"

echo ""
echo "=== Batch Summary ==="
printf "%-60s %-6s %s\n" "Suite" "Status" "Duration"
printf "%-60s %-6s %s\n" "-----" "------" "--------"

for i in "${!SUITE_NAMES[@]}"; do
    if [[ ${SUITE_EXIT_CODES[$i]} -eq 0 ]]; then
        status="PASS"
    else
        status="FAIL"
    fi
    printf "%-60s %-6s %ss\n" "${SUITE_NAMES[$i]}" "$status" "${SUITE_DURATIONS[$i]}"
done

echo ""
echo "Total: $SUITE_COUNT | Passed: $((SUITE_COUNT - FAIL_COUNT)) | Failed: $FAIL_COUNT"

if [[ $FAIL_COUNT -ne 0 ]]; then
    exit 1
fi

exit 0
