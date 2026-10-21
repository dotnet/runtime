#!/bin/bash
# P0.3 probe 2, stage 5 - name the head pauses.
#
# head gates per-GC logging behind the RUNTIME option MMTK_VERBOSE (gc_log.rs:7, gated by
# crate::verbose(level), option declared in util/options.rs).  PLDI gates the equivalent behind
# the COMPILE-TIME feature `log_gc` (args.rs:162 LOG_PER_GC_STATE = cfg!(feature = "log_gc"),
# consumed at plan/immix/global.rs:335).  So head needs no rebuild and PLDI does.
#
# Stage 5a (this script, arg "head"): no rebuild, MMTK_VERBOSE=2 on the shipped head build.
# Stage 5b (arg "pldi"): rebuild PLDI release with the extra `log_gc` feature, restore after.
set -uo pipefail
export PATH=/root/.cargo/bin:$PATH
export LIBCLANG_PATH=/usr/lib/llvm-14/lib
unset MMTK_PLAN
ARM=${1:-head}
ITERS=${2:-5}
HEAP=${3:-2000}

DACAPO=/root/lxr/dacapo/dacapo-2006-10-MR2.jar
OUT=/root/lxr/results/probe2-gclog
mkdir -p "$OUT"
ulimit -c 0

run_head () {
  local B=/root/lxr/head/mmtk-openjdk
  local J=$B/repos/openjdk/build/linux-x86_64-normal-server-release
  cd /root/lxr/dacapo
  env MMTK_PLAN=LXR MMTK_VERBOSE=2 timeout -k 10 900 "$J/jdk/bin/java" -XX:+UseThirdPartyHeap -server \
      -XX:MetaspaceSize=100M -Xms${HEAP}M -Xmx${HEAP}M -Dstall.ms=5.0 \
      -Dbench.tag="head-gclog/hsqldb/$HEAP/1" -Ddacapo.main=Harness \
      -cp "$DACAPO:/root/lxr/harness-tl/cls-head" BenchTL -c TLCallback -n "$ITERS" hsqldb \
      > "$OUT/head-gclog-hsqldb.log" 2>&1
  echo "PASSED=$(grep -c 'PASSED in' "$OUT/head-gclog-hsqldb.log")"
}

run_pldi () {
  local B=/root/lxr/pldi/mmtk-openjdk
  local CONF=linux-x86_64-normal-server-release
  local J=$B/repos/openjdk/build/$CONF
  local SO=$J/jdk/lib/server/libmmtk_openjdk.so
  restore_pldi () {
    echo "-- restoring PLDI binding + shipped release .so --"
    git -C $B checkout -- mmtk/Cargo.toml mmtk/Cargo.lock 2>/dev/null
    cp -a /root/lxr/so-backup/pldi-release-shipped.so "$SO" 2>/dev/null && echo "   .so restored"
  }
  trap restore_pldi EXIT
  git -C $B checkout -- mmtk/Cargo.toml mmtk/Cargo.lock
  # add log_gc alongside whatever the shipped build already selects; the git pin is untouched
  sed -i 's|^\(mmtk = { git = .*rev = "df8d30a39237a5bf5a8e27ca5a6f46acc6080c94"\) }|\1, features = ["log_gc"] }|' \
      $B/mmtk/Cargo.toml
  grep -n '^mmtk = ' $B/mmtk/Cargo.toml
  grep -q 'features = \["log_gc"\]' $B/mmtk/Cargo.toml || { echo "ABORT: feature edit did not apply"; exit 1; }
  local BEFORE; BEFORE=$(stat -c %Y "$SO" 2>/dev/null || echo 0)
  cd $B/repos/openjdk
  make CONF=$CONF THIRD_PARTY_HEAP=$B/openjdk GC_FEATURES=lxr,immix \
       > /root/lxr/logs/probe2-gclog-pldi-make.log 2>&1
  local E=$?; echo "   MAKE_EXIT=$E"
  grep -E 'error(\[|:)' /root/lxr/logs/probe2-gclog-pldi-make.log | head -10
  [ $E -ne 0 ] && { echo "ABORT: build failed"; exit 1; }
  local AFTER; AFTER=$(stat -c %Y "$SO" 2>/dev/null || echo 0)
  ls -l --time-style=+%Y-%m-%dT%H:%M "$SO"
  [ "$AFTER" -gt "$BEFORE" ] || { echo "ABORT: .so not rebuilt (stale library)"; exit 1; }
  cd /root/lxr/dacapo
  env timeout -k 10 900 "$J/jdk/bin/java" -XX:+UseThirdPartyHeap -server \
      -XX:MetaspaceSize=100M -Xms${HEAP}M -Xmx${HEAP}M -Dstall.ms=5.0 \
      -Dbench.tag="pldi-gclog/hsqldb/$HEAP/1" -Ddacapo.main=Harness \
      -cp "$DACAPO:/root/lxr/harness-tl/cls-pldi" BenchTL -c TLCallback -n "$ITERS" hsqldb \
      > "$OUT/pldi-gclog-hsqldb.log" 2>&1
  echo "PASSED=$(grep -c 'PASSED in' "$OUT/pldi-gclog-hsqldb.log")"
}

case "$ARM" in
  head) run_head; L=$OUT/head-gclog-hsqldb.log;;
  pldi) run_pldi; L=$OUT/pldi-gclog-hsqldb.log;;
  *) echo "usage: $0 {head|pldi}"; exit 2;;
esac

echo "===== $ARM: pause kind census ====="
grep -oE 'GC\([0-9]+\) [A-Za-z]+ start' "$L" | awk '{print $2}' | sort | uniq -c | sort -rn
grep -oE '\[pause\] [A-Za-z]+' "$L" | awk '{print $2}' | sort | uniq -c | sort -rn
echo "===== total GC lines ====="
grep -cE 'GC\([0-9]+\).*start|\[pause\]' "$L"
echo "===== stall summary ====="
grep -E "^HICCUPTL|^STALLSUM" "$L"
echo "===== first 25 GC lines ====="
grep -E 'GC\([0-9]+\).*start|\[pause\]' "$L" | head -25
