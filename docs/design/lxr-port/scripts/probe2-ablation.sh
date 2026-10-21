#!/bin/bash
# P0.3 probe 2, stage 2 - single-variable ablation of LAZY_MU_REUSE_BLOCK_SWEEPING.
#
# Hypothesis under test: the lxr-head oracle's hsqldb p99 of 54-56 ms (against the PLDI
# oracle's 0.34-0.40 ms, P0.1-benchmarks.md 6.2) is caused by head having dropped lazy
# sweeping of mutator-reused blocks.  The flag exists only at the PLDI oracle:
#   args.rs:10                       pub const LAZY_MU_REUSE_BLOCK_SWEEPING: bool = cfg!(feature = "lxr_lazy");
#   policy/immix/immixspace.rs:460   if crate::args::LAZY_MU_REUSE_BLOCK_SWEEPING {
#   policy/immix/block_allocation.rs:299
# and has no occurrence at all at the head oracle.
#
# The ablation patches the CONSTANT, not the `lxr_lazy` cargo feature: the feature also
# gates lxr_lazy_decrements, so toggling it would move two variables at once.
#
# Direction of the prediction: turning the flag OFF at PLDI should make PLDI look like
# head (tail grows towards ~54 ms) if the hypothesis holds.
#
# Restores the shipped PLDI release .so and the pristine binding Cargo.toml on exit.
set -uo pipefail
export PATH=/root/.cargo/bin:$PATH
export LIBCLANG_PATH=/usr/lib/llvm-14/lib
unset MMTK_PLAN
ITERS=${1:-5}
INVOCS=${2:-3}
HEAP=${3:-2000}
STALLMS=${4:-5.0}

ORACLE=/root/.cargo/git/checkouts/mmtk-core-10faf03793f704d0/df8d30a
PROBE=/root/lxr/probe2
BINDING=/root/lxr/pldi/mmtk-openjdk
CONF=linux-x86_64-normal-server-release
JDKDIR=$BINDING/repos/openjdk/build/$CONF
SO=$JDKDIR/jdk/lib/server/libmmtk_openjdk.so
DACAPO=/root/lxr/dacapo/dacapo-2006-10-MR2.jar
OUT=/root/lxr/results/probe2-ablation

restore () {
  echo "-- restoring pristine PLDI binding + shipped release .so --"
  git -C $BINDING checkout -- mmtk/Cargo.toml mmtk/Cargo.lock 2>/dev/null
  cp -a /root/lxr/so-backup/pldi-release-shipped.so "$SO" 2>/dev/null && echo "   .so restored"
  grep -n '^mmtk = ' $BINDING/mmtk/Cargo.toml
}
trap restore EXIT

mkdir -p "$PROBE" "$OUT" /root/lxr/logs
echo core > /proc/sys/kernel/core_pattern 2>/dev/null
ulimit -c 0

echo "########## 1. writable copy of the PLDI oracle ##########"
rm -rf $PROBE/mmtk-core
cp -a $ORACLE $PROBE/mmtk-core
chmod -R u+w $PROBE/mmtk-core
cp -a $PROBE/mmtk-core/src/args.rs $PROBE/args.rs.orig

echo
echo "########## 2. ablate ONLY the constant ##########"
python3 - "$PROBE/mmtk-core/src/args.rs" <<'PYEOF'
import sys
p = sys.argv[1]
s = open(p).read()
old = 'pub const LAZY_MU_REUSE_BLOCK_SWEEPING: bool = cfg!(feature = "lxr_lazy");'
new = 'pub const LAZY_MU_REUSE_BLOCK_SWEEPING: bool = false; // P0.3 probe 2 ablation'
assert s.count(old) == 1, "anchor matched %d times" % s.count(old)
open(p, "w").write(s.replace(old, new))
print("ablated OK")
PYEOF
[ $? -ne 0 ] && { echo "ABLATION PATCH FAILED"; exit 1; }
diff -u $PROBE/args.rs.orig $PROBE/mmtk-core/src/args.rs | tee $PROBE/probe2.patch

echo
echo "########## 3. build PLDI release against the ablated core ##########"
git -C $BINDING checkout -- mmtk/Cargo.toml mmtk/Cargo.lock
sed -i 's|^mmtk = { git = .*|mmtk = { path = "/root/lxr/probe2/mmtk-core" }|' $BINDING/mmtk/Cargo.toml
grep -n '^mmtk = ' $BINDING/mmtk/Cargo.toml
cd $BINDING/repos/openjdk
make CONF=$CONF THIRD_PARTY_HEAP=$BINDING/openjdk GC_FEATURES=lxr,immix \
     > /root/lxr/logs/probe2-make.log 2>&1
echo "   MAKE_EXIT=$?"
grep -E 'error(\[|:)' /root/lxr/logs/probe2-make.log | head -10
ls -l --time-style=+%Y-%m-%dT%H:%M "$SO" || { echo "BUILD FAILED - no .so"; exit 1; }
cp -a "$SO" /root/lxr/so-backup/pldi-release-probe2-nolazy.so

echo
echo "########## 4. run hsqldb with the ablated collector ##########"
for inv in $(seq 1 "$INVOCS"); do
  log="$OUT/pldi-nolazy-hsqldb-$HEAP-$inv.log"
  cd /root/lxr/dacapo
  rm -f hs_err_pid*.log core.*
  t0=$(date +%s)
  timeout -k 10 900 "$JDKDIR/jdk/bin/java" -XX:+UseThirdPartyHeap -server \
      -XX:MetaspaceSize=100M -Xms${HEAP}M -Xmx${HEAP}M -Dstall.ms=$STALLMS \
      -Dbench.tag="pldi-nolazy/hsqldb/$HEAP/$inv" -Ddacapo.main=Harness \
      -cp "$DACAPO:/root/lxr/harness-tl/cls-pldi" BenchTL -c TLCallback -n "$ITERS" hsqldb \
      > "$log" 2>&1
  echo "-- inv=$inv ($(( $(date +%s) - t0 ))s, PASSED=$(grep -c 'PASSED in' "$log"))"
  grep -E "^HICCUPTL|^STALLSUM" "$log" | sed 's/^/     /'
done

echo
echo "===== ablated PLDI (no lazy mutator-reuse sweeping) ====="
grep -h "^HICCUPTL" "$OUT"/*.log | sed 's/^/  /'
echo "===== compare with the shipped arms from probe 2 stage 1 ====="
grep -h "^HICCUPTL" /root/lxr/results/probe2-timeline/*.log 2>/dev/null | sed 's/^/  /'
echo "probe2-ablation done -> $OUT"
