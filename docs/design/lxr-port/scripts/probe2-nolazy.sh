#!/bin/bash
# P0.3 probe 2, stage 4 - does head's STW decrement/sweep phase own the hsqldb pauses?
#
# Established so far:
#   - LAZY_DECREMENTS is TRUE at BOTH oracles.
#       PLDI  df8d30a args.rs:49  = cfg!(feature = "lxr_lazy_decrements"), and
#             Cargo.toml:120 lxr = [... "lxr_lazy" ...], :110 lxr_lazy = [... "lxr_lazy_decrements"]
#       head  304ce69 args.rs:111 = !cfg!(feature = "lxr_no_lazy"), and the shipped build sets no
#             GC features at all.
#   - So both defer decrements; what differs is WHERE the deferred work is drained.
#     head has a stop-the-world bucket STWRCDecsAndSweep (work_bucket.rs:416, driven from
#     plan/lxr/global.rs:718,807 and policy/immix/rc_work.rs:168,289) that has no PLDI counterpart.
#
# head exposes `lxr_no_lazy` as a cargo feature, so eager decrements can be selected at build
# time with no source change.  If the 25 stalls/run collapse, the pauses are attributed to the
# deferred-decrement drain; if they survive, that candidate is eliminated too.
#
# NOTE: a first attempt passed GC_FEATURES=lxr_no_lazy and the build FAILED with
#   error: the package 'mmtk_openjdk' does not contain this feature: lxr_no_lazy
# while leaving a stale .so in place, so the run silently re-measured the shipped collector
# and reproduced the baseline exactly.  Hence the mtime guard below.
#
# Rebuilds head RELEASE against the pristine git-pinned oracle - only the mmtk feature set
# changes.  Restores the shipped .so on exit.
set -uo pipefail
export PATH=/root/.cargo/bin:$PATH
export LIBCLANG_PATH=/usr/lib/llvm-14/lib
unset MMTK_PLAN
ITERS=${1:-5}
INVOCS=${2:-2}
HEAP=${3:-2000}

BINDING=/root/lxr/head/mmtk-openjdk
CONF=linux-x86_64-normal-server-release
JDKDIR=$BINDING/repos/openjdk/build/$CONF
SO=$JDKDIR/jdk/lib/server/libmmtk_openjdk.so
DACAPO=/root/lxr/dacapo/dacapo-2006-10-MR2.jar
OUT=/root/lxr/results/probe2-nolazy
mkdir -p "$OUT" /root/lxr/logs

restore () {
  echo "-- restoring shipped head release .so + pristine Cargo files --"
  git -C $BINDING checkout -- mmtk/Cargo.toml mmtk/Cargo.lock 2>/dev/null
  cp -a /root/lxr/so-backup/head-release-shipped.so "$SO" 2>/dev/null && echo "   .so restored"
}
trap restore EXIT

echo core > /proc/sys/kernel/core_pattern 2>/dev/null
ulimit -c 0

echo "########## 1. rebuild head release with mmtk feature lxr_no_lazy ##########"
# GC_FEATURES=lxr_no_lazy does NOT work: the binding's [features] section forwards only a
# handful of mmtk features and lxr_no_lazy is not among them, so cargo rejects it. The
# feature has to be requested on the mmtk dependency itself. The git pin is left untouched,
# so this still builds the declared oracle source 304ce69d.
git -C $BINDING checkout -- mmtk/Cargo.toml mmtk/Cargo.lock
sed -i 's|^\(mmtk = { git = .*rev = "304ce69d43aae87de501111fceb8cbd33173a03a"\) }|\1, features = ["lxr_no_lazy"] }|' \
    $BINDING/mmtk/Cargo.toml
grep -n '^mmtk = ' $BINDING/mmtk/Cargo.toml
grep -q 'features = \["lxr_no_lazy"\]' $BINDING/mmtk/Cargo.toml || { echo "ABORT: feature edit did not apply"; exit 1; }

SO_BEFORE=$(stat -c %Y "$SO" 2>/dev/null || echo 0)
cd $BINDING/repos/openjdk
make CONF=$CONF THIRD_PARTY_HEAP=$BINDING/openjdk \
     > /root/lxr/logs/probe2-nolazy-make.log 2>&1
MAKE_EXIT=$?
echo "   MAKE_EXIT=$MAKE_EXIT"
grep -E 'error(\[|:)' /root/lxr/logs/probe2-nolazy-make.log | head -10
[ $MAKE_EXIT -ne 0 ] && { echo "ABORT: build failed"; exit 1; }
SO_AFTER=$(stat -c %Y "$SO" 2>/dev/null || echo 0)
ls -l --time-style=+%Y-%m-%dT%H:%M "$SO"
[ "$SO_AFTER" -gt "$SO_BEFORE" ] || { echo "ABORT: .so was not rebuilt (mtime unchanged) - a stale library would silently reproduce the baseline"; exit 1; }
cp -a "$SO" /root/lxr/so-backup/head-release-probe2-nolazy.so

echo
echo "########## 2. run hsqldb with eager decrements ##########"
for inv in $(seq 1 "$INVOCS"); do
  log="$OUT/head-nolazy-hsqldb-$HEAP-$inv.log"
  cd /root/lxr/dacapo
  rm -f hs_err_pid*.log core.*
  t0=$(date +%s)
  env MMTK_PLAN=LXR timeout -k 10 900 "$JDKDIR/jdk/bin/java" -XX:+UseThirdPartyHeap -server \
      -XX:MetaspaceSize=100M -Xms${HEAP}M -Xmx${HEAP}M -Dstall.ms=5.0 \
      -Dbench.tag="head-nolazy/hsqldb/$HEAP/$inv" -Ddacapo.main=Harness \
      -cp "$DACAPO:/root/lxr/harness-tl/cls-head" BenchTL -c TLCallback -n "$ITERS" hsqldb \
      > "$log" 2>&1
  echo "-- inv=$inv ($(( $(date +%s) - t0 ))s, PASSED=$(grep -c 'PASSED in' "$log"))"
  grep -E "^HICCUPTL|^STALLSUM" "$log" | sed 's/^/     /'
done

echo
echo "===== head, eager decrements (lxr_no_lazy) ====="
grep -h "^STALLSUM" "$OUT"/*.log | sed 's/^/  /'
echo "===== head, shipped (lazy decrements, the baseline) ====="
grep -h "^STALLSUM" /root/lxr/results/probe2-timeline/head-*.log 2>/dev/null | sed 's/^/  /'
echo "===== PLDI, shipped ====="
grep -h "^STALLSUM" /root/lxr/results/probe2-timeline/pldi-*.log 2>/dev/null | sed 's/^/  /'
echo "probe2-nolazy done -> $OUT"
