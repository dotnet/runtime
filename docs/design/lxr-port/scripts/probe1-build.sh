#!/bin/bash
# P0.3 probe 1 - re-instate the PLDI zero-RC barrier invariant at the lxr-head ORACLE.
#
# Why: at the head oracle (mmtk-core 304ce69d) src/plan/lxr/barrier.rs has NO rc-count
# assertion, so P0.1's "HEAD never trips the zero rc count assertion" is unfalsified
# rather than verified.  This build adds PLDI's invariant, expressed in head's API, so
# the claim can actually be tested.
#
# Provenance of anything produced here: [obs-override] - instrumented, never-shipped build.
#
# Leaves /root/lxr/{pldi,head} sources pristine (Cargo.toml/lock restored at the end) and
# never touches C:\github\lxr-reference.
set -uo pipefail
export PATH=/root/.cargo/bin:$PATH
export LIBCLANG_PATH=/usr/lib/llvm-14/lib
unset MMTK_PLAN

ORACLE=/root/.cargo/git/checkouts/mmtk-core-91cf05d634be0a1e/304ce69
PROBE=/root/lxr/probe1
BINDING=/root/lxr/head/mmtk-openjdk
CONF=linux-x86_64-normal-server-fastdebug

mkdir -p $PROBE /root/lxr/logs /root/lxr/so-backup

echo "########## 0. verify the oracle source ##########"
ls -d $ORACLE || exit 1
echo "oracle .rs files: $(find $ORACLE/src -name '*.rs' | wc -l)"

echo
echo "########## 1. copy the oracle into a writable probe tree ##########"
rm -rf $PROBE/mmtk-core
cp -a $ORACLE $PROBE/mmtk-core
chmod -R u+w $PROBE/mmtk-core
cp -a $PROBE/mmtk-core/src/plan/lxr/barrier.rs $PROBE/barrier.rs.orig
echo "copied -> $PROBE/mmtk-core"

echo
echo "########## 2. patch ONLY LXRFieldBarrierSemantics::slow ##########"
python3 - "$PROBE/mmtk-core/src/plan/lxr/barrier.rs" <<'PYEOF'
import sys
p = sys.argv[1]
s = open(p).read()
old = """        old: Option<ObjectReference>,
    ) {
        // Reference counting
"""
new = """        old: Option<ObjectReference>,
    ) {
        // P0.3 probe 1: PLDI's barrier invariant, expressed in head's API.
        // PLDI df8d30a3 src/plan/barriers.rs:314-319 asserts
        //     old.is_null() || rc::count(old) != 0
        // head models "null" as None, so the null disjunct becomes the `if let`.
        #[cfg(any(feature = "sanity", debug_assertions))]
        if let Some(old_obj) = old {
            assert!(
                self.lxr.rc.count(old_obj) != 0,
                "zero rc count {:?} (slot {:?})",
                old_obj,
                slot
            );
        }
        // Reference counting
"""
assert s.count(old) == 1, "anchor matched %d times, expected 1" % s.count(old)
open(p, "w").write(s.replace(old, new))
print("patched OK")
PYEOF
[ $? -ne 0 ] && { echo "PATCH FAILED"; exit 1; }

echo
echo "-- patch diff --"
diff -u $PROBE/barrier.rs.orig $PROBE/mmtk-core/src/plan/lxr/barrier.rs | tee $PROBE/probe1.patch
echo "-- end diff --"

echo
echo "########## 3. point the head binding at the probe tree ##########"
git -C $BINDING checkout -- mmtk/Cargo.toml mmtk/Cargo.lock
sed -i 's|^mmtk = { git = .*|mmtk = { path = "/root/lxr/probe1/mmtk-core" }|' $BINDING/mmtk/Cargo.toml
grep -n '^mmtk = ' $BINDING/mmtk/Cargo.toml

echo
echo "########## 4. build fastdebug (head config: NO GC_FEATURES) ##########"
cd $BINDING/repos/openjdk
make CONF=$CONF THIRD_PARTY_HEAP=$BINDING/openjdk > /root/lxr/logs/probe1-make.log 2>&1
echo "   MAKE_EXIT=$?"
grep -E '^cargo build' /root/lxr/logs/probe1-make.log | tail -1
grep -E 'error(\[|:)' /root/lxr/logs/probe1-make.log | head -10
ls -l --time-style=+%Y-%m-%dT%H:%M build/$CONF/jdk/lib/server/libmmtk_openjdk.so 2>/dev/null

echo
echo "########## 5. save the probe .so, restore the binding ##########"
cp -a build/$CONF/jdk/lib/server/libmmtk_openjdk.so /root/lxr/so-backup/head-fastdebug-probe1.so 2>/dev/null \
  && echo "saved -> /root/lxr/so-backup/head-fastdebug-probe1.so"
git -C $BINDING checkout -- mmtk/Cargo.toml mmtk/Cargo.lock
grep -n '^mmtk = ' $BINDING/mmtk/Cargo.toml
echo "probe1-build done"
