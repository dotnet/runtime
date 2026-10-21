#!/bin/bash
# Compile the native validation program from the worktree and run only the given selectors.
# EXTRA holds extra compiler flags (for example -DLXR_S4_NO_REFRESH_CONTROL); OUTNAME selects the output directory.
# usage: native-one.sh <Debug|Release> <selector...>
REPO=${REPO:-/mnt/c/Users/konradkokosa/.copilot/repos/copilot-worktrees/runtime-fork/kkokosa-automatic-tribble}
CFG=$1
shift
OUT=${P17_ROOT:-/root/p17s6}/native/one-$CFG${OUTNAME:+-$OUTNAME}
mkdir -p $OUT
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
clang++ -std=c++17 -pthread -Wall -Wextra -Werror -Wno-unused-parameter -Wno-invalid-offsetof "${FLAGS[@]}" \
  -DBUILD_AS_STANDALONE -DTARGET_UNIX -DHOST_UNIX -DTARGET_LINUX -DTARGET_AMD64 -DHOST_AMD64 -DTARGET_64BIT -DHOST_64BIT \
  -DLXR_SIDE_METADATA_TEST_PLATFORM -DLXR_ENABLE_LAZY_COMPLETION_COUNTER $EXTRA \
  "-I$GC" "-I$GC/env" "-I$REPO/src/native" "-I$REPO/src/native/inc" "${FILES[@]}" -o "$OUT/lxr-rc-validation" 2>&1 | head -60
for sel in "$@"; do
  [ "$sel" = "default" ] && sel=""
  echo "== ${sel:-default}"
  timeout 900 $OUT/lxr-rc-validation $sel 2>&1 | grep -v '^growth_failure' | tail -${TAILN:-12}
done
