#!/usr/bin/env bash
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
configuration="${1:-Debug}"
output="${2:-$root/artifacts/P4/vertical-linux-$configuration}"
control="${3:-None}"
mkdir -p "$output"
flags=(-O0 -g -DDEBUG -D_DEBUG)
if [[ "$configuration" == Release ]]; then
    flags=(-O2 -DNDEBUG)
elif [[ "$configuration" != Debug ]]; then
    echo "Unsupported configuration: $configuration" >&2
    exit 1
fi
case "$control" in
    None) ;;
    EarlyRoot) flags+=(-DLXR_RC_EARLY_ROOT_CONTROL -Wno-unused-function) ;;
    SweepQuery) flags+=(-DLXR_RC_SWEEP_QUERY_CONTROL -Wno-unused-function) ;;
    TraceSatb) flags+=(-DLXR_TRACE_SKIP_SATB_CONTROL) ;;
    TraceDeletion) flags+=(-DLXR_TRACE_SKIP_DELETION_CONTROL) ;;
    PredictorUpdate) flags+=(-DLXR_POLICY_WRONG_UPDATE_CONTROL) ;;
    TriggerOrder) flags+=(-DLXR_POLICY_WRONG_ORDER_CONTROL) ;;
    Lifetime) flags+=(-DLXR_PROBE_SKIP_RECLAIM_CONTROL) ;;
    StorageDiagnostics) flags+=(-DLXR_STORAGE_SKIP_COPY_CONTROL) ;;
    ObjectInterior) flags+=(-DLXR_OBJECT_INTERIOR_CONTROL) ;;
    PendingRetirement) flags+=(-DLXR_RETIRE_PENDING_CONTROL) ;;
    MatureStale) flags+=(-DLXR_REMSET_STALE_CONTROL) ;;
    MatureReplay) flags+=(-DLXR_REMSET_REPLAY_CONTROL) ;;
    MatureShape) flags+=(-DLXR_REMSET_SHAPE_CONTROL) ;;
    MatureScan) flags+=(-DLXR_MATURE_SCAN_REBASE_CONTROL) ;;
    MatureReaderCompletion) flags+=(-DLXR_MATURE_READER_COMPLETION_CONTROL) ;;
    MatureDeadScan) flags+=(-DLXR_MATURE_DEAD_SCAN_CONTROL) ;;
    MatureEarlyRelease) flags+=(-DLXR_MATURE_EARLY_RELEASE_CONTROL) ;;
    MatureCount) flags+=(-DLXR_MATURE_COUNT_CONTROL) ;;
    MaturePredictor) flags+=(-DLXR_MATURE_PREDICTOR_CONTROL) ;;
    ReserveUnits) flags+=(-DLXR_RESERVE_UNITS_CONTROL) ;;
    LogPublication) flags+=(-DLXR_LOG_PUBLICATION_CONTROL) ;;
    LogDecode) flags+=(-DLXR_LOG_DECODE_CONTROL) ;;
    LogCapacity) flags+=(-DLXR_LOG_CAPACITY_CONTROL) ;;
    LogReuse) flags+=(-DLXR_LOG_REUSE_CONTROL) ;;
    LogDrop) flags+=(-DLXR_LOG_DROP_CONTROL) ;;
    TraceBatchDrop) flags+=(-DLXR_TRACE_BATCH_DROP_CONTROL) ;;
    TraceBatchUnbatched) flags+=(-DLXR_TRACE_BATCH_UNBATCHED_CONTROL) ;;
    TraceBatchEarly) flags+=(-DLXR_TRACE_BATCH_EARLY_CONTROL) ;;
    *) echo "Unsupported control: $control" >&2; exit 1 ;;
esac
gc="$root/src/coreclr/gc"
sources=(
    side_metadata.cpp immix_block.cpp immix_allocation.cpp reference_count.cpp
    large_object_space.cpp lxr_work_packet.cpp lxr_work_scheduler.cpp
    lxr_pause_schedule.cpp lxr_pause_coordinator.cpp lxr_completion_counter.cpp
    lxr_reference_counting.cpp
)
for index in "${!sources[@]}"; do
    sources[$index]="$gc/${sources[$index]}"
done
sources+=(
    "$root/docs/design/lxr-port/P2.5/large-object-space-test-platform.cpp"
    "$root/docs/design/lxr-port/P4/lxr-rc-validation.cpp"
)
sha256sum "${sources[@]}" "$gc"/*.h > "$output/source-identities.txt"
command=(
    clang++ -std=c++17 -pthread -Wall -Wextra -Werror -Wno-unused-parameter -Wno-invalid-offsetof
    "${flags[@]}" -DBUILD_AS_STANDALONE -DTARGET_UNIX -DHOST_UNIX -DTARGET_LINUX
    -DTARGET_AMD64 -DHOST_AMD64 -DTARGET_64BIT -DHOST_64BIT
    -DLXR_SIDE_METADATA_TEST_PLATFORM -DLXR_ENABLE_LAZY_COMPLETION_COUNTER
    "-I$gc" "-I$gc/env" "-I$root/src/native" "-I$root/src/native/inc"
    "${sources[@]}" -o "$output/lxr-rc-validation"
)
printf '%q ' "${command[@]}" > "$output/compile-command.txt"
printf '\n' >> "$output/compile-command.txt"
"${command[@]}" > "$output/build.log" 2>&1
status=0
selectors=()
case "$control" in
    MatureStale|MatureReplay) selectors=(--mature-remset) ;;
    MatureShape) selectors=(--mature-remset-wrap) ;;
    MatureScan|MatureReaderCompletion) selectors=(--mature-reader) ;;
    MatureDeadScan) selectors=(--mature-completed-scan) ;;
    MatureEarlyRelease|MatureCount|MaturePredictor) selectors=(--mature) ;;
    ReserveUnits) selectors=(--p8-policy) ;;
    Log*) selectors=(--log-storage) ;;
    TraceBatch*) selectors=(--trace-batching) ;;
esac
"$output/lxr-rc-validation" "${selectors[@]}" > "$output/run.log" 2>&1 || status=$?
if [[ "$control" != None ]]; then
    if [[ "$status" == 0 ]] || ! grep -q 'FAIL:' "$output/run.log"; then
        echo "Failure control $control did not detect its injected violation." >&2
        exit 1
    fi
    echo "CONTROL $control detected: exit=$status"
    exit 0
fi
if [[ "$status" != 0 ]]; then
    cat "$output/run.log"
    exit "$status"
fi
cat "$output/run.log"
grep -qE '^PASS: [1-9][0-9]*/[1-9][0-9]* integrated P4 checks$' "$output/run.log"
