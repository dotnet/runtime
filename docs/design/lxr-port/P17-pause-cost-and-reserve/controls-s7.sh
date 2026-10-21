#!/bin/bash
# P17 S7 controls. Build the native validation program once for each control define and run the selectors that must
# notice the broken rule. Each control must make at least one selector print FAIL or end abnormally.
# usage: controls-s7.sh <repo-root> <Release|Debug> <output-dir>
REPO=${1:?repo root}
CFG=${2:-Release}
OUT=${3:-/tmp/controls-s7}
mkdir -p "$OUT"
case "$CFG" in
  Debug) FLAGS=(-O0 -g -DDEBUG -D_DEBUG);;
  *) FLAGS=(-O2 -DNDEBUG);;
esac
GC="$REPO/src/coreclr/gc"
SOURCES=(side_metadata.cpp immix_block.cpp immix_allocation.cpp reference_count.cpp large_object_space.cpp
  lxr_work_packet.cpp lxr_work_scheduler.cpp lxr_pause_schedule.cpp lxr_pause_coordinator.cpp
  lxr_completion_counter.cpp lxr_reference_counting.cpp)
FILES=()
for s in "${SOURCES[@]}"; do FILES+=("$GC/$s"); done
FILES+=("$REPO/docs/design/lxr-port/P2.5/large-object-space-test-platform.cpp" "$REPO/docs/design/lxr-port/P4/lxr-rc-validation.cpp")
# control -> selectors
declare -A SELECTORS=(
  [NO_DEDUPE]="--s7-pins"
  [NO_CLEAR]="--s7-pins"
  [UNSAFE_ADD]="--s7-pins"
  [IGNORE_PIN]="--s7-pins --mature"
  [NO_BLOCK_BYTE]="--s7-pins"
  [NO_BLOCK_CLEAR]="--s7-pins"
  [NO_RETAIN]="--mature --mature-retained-owner"
  [PIN_INTERIOR]="--s7-pins"
  [POH_NORMAL]="--s7-pins"
  [POH_RECYCLE]="--s7-pins"
  [SCAN_MOVE]="--mature-reader --mature-completed-scan --trace"
  [SCAN_LINK]="--trace --mature-reader --mature-completed-scan --lazy-large-release"
)
ORDER=(NO_DEDUPE NO_CLEAR UNSAFE_ADD IGNORE_PIN NO_BLOCK_BYTE NO_BLOCK_CLEAR NO_RETAIN PIN_INTERIOR POH_NORMAL POH_RECYCLE SCAN_MOVE SCAN_LINK)
[ -n "${ONLY:-}" ] && ORDER=($ONLY)
for c in "${ORDER[@]}"; do
  exe="$OUT/ctl-$c"
  clang++ -std=c++17 -pthread -Wall -Wextra -Werror -Wno-unused-parameter -Wno-invalid-offsetof "${FLAGS[@]}" \
    -DBUILD_AS_STANDALONE -DTARGET_UNIX -DHOST_UNIX -DTARGET_LINUX -DTARGET_AMD64 -DHOST_AMD64 -DTARGET_64BIT -DHOST_64BIT \
    -DLXR_SIDE_METADATA_TEST_PLATFORM -DLXR_ENABLE_LAZY_COMPLETION_COUNTER "-DLXR_S7_${c}_CONTROL" \
    "-I$GC" "-I$GC/env" "-I$REPO/src/native" "-I$REPO/src/native/inc" "${FILES[@]}" -o "$exe" > "$OUT/ctl-$c.build.log" 2>&1
  if [ ! -x "$exe" ]; then echo "control LXR_S7_${c}_CONTROL: BUILD FAILED"; tail -5 "$OUT/ctl-$c.build.log"; continue; fi
  for sel in ${SELECTORS[$c]}; do
    timeout 900 "$exe" $sel > "$OUT/ctl-$c$sel.log" 2>&1
    code=$?
    line=$(grep -E '^(PASS|FAIL):' "$OUT/ctl-$c$sel.log" | tail -1)
    echo "control LXR_S7_${c}_CONTROL $sel exit=$code :: ${line:-no result line (abnormal end)}"
  done
done
