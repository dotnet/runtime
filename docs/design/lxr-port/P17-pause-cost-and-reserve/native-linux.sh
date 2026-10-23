#!/bin/bash
# Compile the native LXR validation program and run every selector. Prints one line per selector.
# usage: native-linux.sh <repo-root-as-WSL-path> <Debug|Release> <output-dir> [extra clang flags]
set -u
REPO="$1"
CFG="$2"
OUT="$3"
shift 3
mkdir -p "$OUT"
case "$CFG" in
  Debug) FLAGS=(-O0 -g -DDEBUG -D_DEBUG);;
  Release) FLAGS=(-O2 -DNDEBUG);;
  *) echo "bad config"; exit 2;;
esac
GC="$REPO/src/coreclr/gc"
rm -f "$OUT/lxr-rc-validation"
SOURCES=(side_metadata.cpp immix_block.cpp immix_allocation.cpp reference_count.cpp large_object_space.cpp
  lxr_work_packet.cpp lxr_work_scheduler.cpp lxr_pause_schedule.cpp lxr_pause_coordinator.cpp
  lxr_completion_counter.cpp lxr_reference_counting.cpp)
FILES=()
for s in "${SOURCES[@]}"; do FILES+=("$GC/$s"); done
FILES+=("$REPO/docs/design/lxr-port/P2.5/large-object-space-test-platform.cpp" "$REPO/docs/design/lxr-port/P4/lxr-rc-validation.cpp")
clang++ -std=c++17 -pthread -Wall -Wextra -Werror -Wno-unused-parameter -Wno-invalid-offsetof "${FLAGS[@]}" \
  -DBUILD_AS_STANDALONE -DTARGET_UNIX -DHOST_UNIX -DTARGET_LINUX -DTARGET_AMD64 -DHOST_AMD64 -DTARGET_64BIT -DHOST_64BIT \
  -DLXR_SIDE_METADATA_TEST_PLATFORM -DLXR_ENABLE_LAZY_COMPLETION_COUNTER "$@" \
  "-I$GC" "-I$GC/env" "-I$REPO/src/native" "-I$REPO/src/native/inc" "${FILES[@]}" -o "$OUT/lxr-rc-validation" > "$OUT/build.log" 2>&1
echo "build exit=$?"
[ -x "$OUT/lxr-rc-validation" ] || { tail -20 "$OUT/build.log"; exit 1; }
failed=0
for sel in "" --log-storage --mature --mature-remset --mature-reader --mature-retained-owner --mature-released-candidate \
    --mature-headroom --mature-completed-scan --mature-remset-whole --mature-remset-wrap --mature-remset-same-shape \
    --mature-remset-boundaries --nursery --nursery-reader --root-batching --trace-batching --aux-retirement \
    --object-planes --storage-diagnostics --lifetime --p8-policy --p8-storage --trace --first-store-churn --inline-metadata --lazy-large-release --s3-bookkeeping --s4-sweep --s5b-overshoot --s6-registry --s7-pins --fresh-block-race --d2-spans --f1-trace --f5-policy --f4-scheduler --f2-pause --f4b-scheduler --f8-lazy --f10-rc; do
  name=${sel:-default}
  timeout 1500 "$OUT/lxr-rc-validation" $sel > "$OUT/run$name.log" 2>&1
  code=$?
  line=$(grep -E '^(PASS|FAIL):' "$OUT/run$name.log" | tail -1)
  flip=$(grep -E '^flip_overlaps' "$OUT/run$name.log" | tail -1)
  echo "$CFG $name exit=$code :: $line :: $flip"
  [ $code -eq 0 ] || failed=1
done
echo "native $CFG failed=$failed"
