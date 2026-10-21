#!/bin/bash
# P0.3 probe 1b - POSITIVE CONTROL for probe 1.
#
# Why: probe 1 reported 48/48 runs clean with PLDI's zero-RC invariant re-instated at
# the head oracle.  A null result is only evidence if the assertion site was actually
# REACHED.  Probe 1 proved the assertion was COMPILED IN (its string is in the .so);
# it did not prove it EXECUTED.  Without this control, 48/48 clean is exactly the same
# shape of claim as P0.1's vacuous "head never trips it".
#
# Method: same tree, same configuration, one line changed - the invariant is replaced
# by a reach counter that logs at every power of two.  The run completes normally and
# reports how many times the site was executed.  If the count is large, probe 1's null
# result is a real negative; if the site is never reached, probe 1's null result is
# vacuous and must be reported as such.
#
# A counter that logs is preferred over one that aborts: it yields a magnitude rather
# than a bit, and it avoids the WSL crash-capture pipe entirely.
#
# Provenance: [obs-override] - instrumented, never-shipped build.  Never touches
# C:\github\lxr-reference.
set -uo pipefail
export PATH="/root/.cargo/bin:$PATH"
export LIBCLANG_PATH=/usr/lib/llvm-14/lib
unset MMTK_PLAN

ORACLE=/root/.cargo/git/checkouts/mmtk-core-91cf05d634be0a1e/304ce69
PROBE=/root/lxr/probe1b
BINDING=/root/lxr/head/mmtk-openjdk
CONF=linux-x86_64-normal-server-fastdebug
SO=$BINDING/repos/openjdk/build/$CONF/jdk/lib/server/libmmtk_openjdk.so

mkdir -p $PROBE /root/lxr/logs /root/lxr/so-backup

echo "########## 1. fresh copy of the oracle ##########"
rm -rf $PROBE/mmtk-core
cp -a $ORACLE $PROBE/mmtk-core
chmod -R u+w $PROBE/mmtk-core
cp -a $PROBE/mmtk-core/src/plan/lxr/barrier.rs $PROBE/barrier.rs.orig

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
        // P0.3 probe 1b: POSITIVE CONTROL.  Same site and same cfg gate as probe 1, but
        // the invariant is replaced by a reach counter logged at each power of two, so
        // the run completes and reports how often the site probe 1 tested is executed.
        #[cfg(any(feature = "sanity", debug_assertions))]
        if let Some(old_obj) = old {
            static PROBE1B_REACHES: std::sync::atomic::AtomicUsize =
                std::sync::atomic::AtomicUsize::new(0);
            let n = PROBE1B_REACHES.fetch_add(1, std::sync::atomic::Ordering::Relaxed) + 1;
            if n.is_power_of_two() {
                eprintln!(
                    "probe1b positive control: slow path reached {} times (last old={:?}, rc={})",
                    n,
                    old_obj,
                    self.lxr.rc.count(old_obj)
                );
            }
        }
        // Reference counting
"""
assert s.count(old) == 1, "anchor matched %d times, expected 1" % s.count(old)
open(p, "w").write(s.replace(old, new))
print("patched OK")
PYEOF
[ $? -ne 0 ] && { echo "PATCH FAILED"; exit 1; }
diff -u $PROBE/barrier.rs.orig $PROBE/mmtk-core/src/plan/lxr/barrier.rs | tee $PROBE/probe1b.patch

echo
echo "########## 3. point the head binding at the probe tree ##########"
git -C $BINDING checkout -- mmtk/Cargo.toml mmtk/Cargo.lock
sed -i "s|^mmtk = { git = .*|mmtk = { path = \"$PROBE/mmtk-core\" }|" $BINDING/mmtk/Cargo.toml
grep -n '^mmtk = ' $BINDING/mmtk/Cargo.toml

echo
echo "########## 4. build fastdebug (head config: NO GC_FEATURES, NO sanity) ##########"
BEFORE=$(stat -c %Y "$SO" 2>/dev/null || echo 0)
cd $BINDING/repos/openjdk
make CONF=$CONF THIRD_PARTY_HEAP=$BINDING/openjdk > /root/lxr/logs/probe1b-make.log 2>&1
MAKE_EXIT=$?
AFTER=$(stat -c %Y "$SO" 2>/dev/null || echo 0)
echo "   MAKE_EXIT=$MAKE_EXIT  so_mtime ${BEFORE} -> ${AFTER}"
grep -E 'error(\[|:)' /root/lxr/logs/probe1b-make.log | head -10
# never trust the exit code alone: the library must also be newer than it was
if [ $MAKE_EXIT -ne 0 ] || [ "$AFTER" -le "$BEFORE" ]; then
  echo "BUILD FAILED OR STALE - aborting so a stale .so cannot be measured"
  git -C $BINDING checkout -- mmtk/Cargo.toml mmtk/Cargo.lock
  exit 1
fi

echo
echo "########## 5. confirm the control string is in the binary ##########"
strings "$SO" | grep -c 'probe1b positive control'
cp -a "$SO" /root/lxr/so-backup/head-fastdebug-probe1b.so && echo "saved probe1b .so"
git -C $BINDING checkout -- mmtk/Cargo.toml mmtk/Cargo.lock
grep -n '^mmtk = ' $BINDING/mmtk/Cargo.toml
echo "probe1b-build done"
