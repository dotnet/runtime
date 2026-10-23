#!/bin/bash
# P22 (D2): the controls of the bump-allocation tests. Each control is a build define that breaks one behaviour; the
# selector --d2-spans must then FAIL. The first line (no define) must PASS.
# usage: controls-d2.sh <repo-root-as-WSL-path> <output-dir> [Debug|Release]
set -u
REPO="$1"
OUT="$2"
CFG="${3:-Debug}"
mkdir -p "$OUT"
case "$CFG" in
  Debug) FLAGS=(-O0 -g -DDEBUG -D_DEBUG);;
  Release) FLAGS=(-O2 -DNDEBUG);;
  *) echo "bad config"; exit 2;;
esac
GC="$REPO/src/coreclr/gc"
SOURCES=(side_metadata.cpp immix_block.cpp immix_allocation.cpp reference_count.cpp large_object_space.cpp
  lxr_work_packet.cpp lxr_work_scheduler.cpp lxr_pause_schedule.cpp lxr_pause_coordinator.cpp
  lxr_completion_counter.cpp lxr_reference_counting.cpp)
FILES=()
for s in "${SOURCES[@]}"; do FILES+=("$GC/$s"); done
FILES+=("$REPO/docs/design/lxr-port/P2.5/large-object-space-test-platform.cpp" "$REPO/docs/design/lxr-port/P4/lxr-rc-validation.cpp")
failed=0
for control in "" LXR_VALID_CLEAR_CONTROL LXR_D2_NO_MARK_CONTROL \
    LXR_D2_SPAN_TO_LIMIT_CONTROL LXR_D2_DETACH_DROP_CONTROL LXR_D2_NO_SPAN_LOOKUP_CONTROL \
    LXR_D2_NO_PROMOTE_VALID_CONTROL LXR_D2_NO_PINNED_VALID_CONTROL; do
  name=${control:-baseline}
  DEFINE=()
  [ -n "$control" ] && DEFINE=("-D$control")
  clang++ -std=c++17 -pthread -Wall -Wextra -Werror -Wno-unused-parameter -Wno-invalid-offsetof "${FLAGS[@]}" \
    -DBUILD_AS_STANDALONE -DTARGET_UNIX -DHOST_UNIX -DTARGET_LINUX -DTARGET_AMD64 -DHOST_AMD64 -DTARGET_64BIT -DHOST_64BIT \
    -DLXR_SIDE_METADATA_TEST_PLATFORM -DLXR_ENABLE_LAZY_COMPLETION_COUNTER "${DEFINE[@]}" \
    "-I$GC" "-I$GC/env" "-I$REPO/src/native" "-I$REPO/src/native/inc" "${FILES[@]}" -o "$OUT/d2-$name" > "$OUT/build-$name.log" 2>&1
  if [ ! -x "$OUT/d2-$name" ]; then echo "$name: build failed"; tail -5 "$OUT/build-$name.log"; failed=1; continue; fi
  timeout 600 "$OUT/d2-$name" --d2-spans > "$OUT/run-$name.log" 2>&1
  code=$?
  summary=$(grep -E '^(PASS|FAIL):' "$OUT/run-$name.log" | tail -1)
  broken=$(grep -E '^FAIL: ' "$OUT/run-$name.log" | grep -v -E '^FAIL: [0-9]+/' | sort -u | head -3 | tr '\n' '|')
  if [ -z "$control" ]; then
    [ $code -eq 0 ] || failed=1
    echo "baseline exit=$code :: $summary"
  else
    [ $code -ne 0 ] || failed=1
    echo "$control exit=$code :: $summary :: $broken"
  fi
done
echo "controls failed=$failed"
