#!/bin/bash
# Root capture fix: control. The program is built with LXR_FRESH_NURSERY_RACE_CONTROL (the locked acquire of a fresh
# mutator block does not check the nursery again). The selector --fresh-block-race must print FAIL.
# usage: controls-rcf.sh <repo-root> <Release|Debug> <output-dir>
REPO=${1:?repo root}
CFG=${2:-Release}
OUT=${3:-/tmp/controls-rcf}
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
exe="$OUT/ctl-fresh-nursery-race"
clang++ -std=c++17 -pthread -Wall -Wextra -Werror -Wno-unused-parameter -Wno-invalid-offsetof "${FLAGS[@]}" \
  -DBUILD_AS_STANDALONE -DTARGET_UNIX -DHOST_UNIX -DTARGET_LINUX -DTARGET_AMD64 -DHOST_AMD64 -DTARGET_64BIT -DHOST_64BIT \
  -DLXR_SIDE_METADATA_TEST_PLATFORM -DLXR_ENABLE_LAZY_COMPLETION_COUNTER -DLXR_FRESH_NURSERY_RACE_CONTROL \
  "-I$GC" "-I$GC/env" "-I$REPO/src/native" "-I$REPO/src/native/inc" "${FILES[@]}" -o "$exe" > "$OUT/ctl.build.log" 2>&1
if [ ! -x "$exe" ]; then echo "control LXR_FRESH_NURSERY_RACE_CONTROL: BUILD FAILED"; tail -5 "$OUT/ctl.build.log"; exit 1; fi
timeout 900 "$exe" --fresh-block-race > "$OUT/ctl.log" 2>&1
code=$?
line=$(grep -E '^(PASS|FAIL):' "$OUT/ctl.log" | tail -1)
echo "control LXR_FRESH_NURSERY_RACE_CONTROL --fresh-block-race exit=$code :: ${line:-no result line (abnormal end)}"
