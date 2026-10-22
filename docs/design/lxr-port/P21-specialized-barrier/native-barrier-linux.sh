#!/bin/bash
# Build and run the native test of the LXR write barrier assembly, then build it again with each control and require that
# the named check fails.
# usage: native-barrier-linux.sh <repo-root-as-WSL-path> <output-dir>
set -u
REPO="$1"
OUT="$2"
mkdir -p "$OUT"
GC="$REPO/src/coreclr/gc"
TEST="$REPO/docs/design/lxr-port/P21-specialized-barrier/lxr-write-barrier-test.cpp"

build() {  # build <name> <asm defines> <cxx defines>
  local name=$1 asmdef=$2 cxxdef=$3
  clang -c -x assembler-with-cpp -DHOST_AMD64 -DTARGET_AMD64 -DTARGET_UNIX -DHOST_UNIX $asmdef \
    -I"$REPO/src/coreclr/pal/inc" -I"$REPO/src/coreclr/pal/inc/unix" "$GC/amd64/lxr_writebarrier.S" -o "$OUT/$name.asm.o" > "$OUT/$name.build.log" 2>&1 || return 1
  clang++ -std=c++17 -O2 -mxsave -pthread -Wall -Wextra -Werror -Wno-unused-parameter -I"$GC" $cxxdef \
    "$TEST" "$GC/lxr_write_barrier.cpp" "$OUT/$name.asm.o" -o "$OUT/$name" >> "$OUT/$name.build.log" 2>&1 || return 1
}

failed=0
build normal "" "" || { echo "build failed"; tail -20 "$OUT/normal.build.log"; exit 1; }
"$OUT/normal" > "$OUT/normal.log" 2>&1
code=$?
cat "$OUT/normal.log" | grep -E '^(PASS|FAIL):' | tail -40
[ $code -eq 0 ] || failed=1

control() {  # control <name> <asm defines> <cxx defines> <check that must fail>
  build "$1" "$2" "$3" || { echo "control $1: build failed"; tail -20 "$OUT/$1.build.log"; failed=1; return; }
  "$OUT/$1" > "$OUT/$1.log" 2>&1
  if grep -q "^FAIL: $4" "$OUT/$1.log"; then echo "control $1: the check '$4' fails as required"; else echo "control $1: the check '$4' did NOT fail"; failed=1; fi
}
control no-vector-save "-DLXR_WB_CONTROL_NO_VECTOR_SAVE" "" vector
control misalign "-DLXR_WB_CONTROL_MISALIGN" "" alignment
control no-store-after-slow "-DLXR_WB_CONTROL_NO_STORE_AFTER_SLOW" "" store-after-slow
control read-old-after-claim "" "-DLXR_WBTEST_CONTROL_READ_OLD_AFTER_CLAIM" old-value-race
control no-patch "" "-DLXR_WBTEST_CONTROL_NO_PATCH" patch
echo "barrier native test failed=$failed"
exit $failed
