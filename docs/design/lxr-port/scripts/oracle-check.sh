#!/bin/bash
# P0.1 - resolve whether each binding compiles against its TASK-NAMED oracle
# (as opposed to only against the mmtk-core revision the binding itself pins).
set -uo pipefail
cd /root
export PATH=/root/.cargo/bin:$PATH
export CARGO_NET_GIT_FETCH_WITH_CLI=true

REV=$1          # pldi | head
TOOLCHAIN=$2    # nightly-2021-10-29 | 1.92.0
FEATURES=${3:-} # e.g. lxr, or empty

BIND=/root/lxr/$REV/mmtk-openjdk/mmtk
CORE=/root/lxr/$REV/mmtk-core

cd "$BIND"
[ -f Cargo.toml.orig ] || cp Cargo.toml Cargo.toml.orig

# Replace the git dependency on mmtk-core with a path dependency on the ORACLE checkout.
python3 - "$CORE" <<'PY'
import re, sys
core = sys.argv[1]
src = open('Cargo.toml.orig').read()
out, n = re.subn(
    r'^mmtk = \{ git = .*$',
    'mmtk = { path = "%s" }' % core,
    src, flags=re.M)
assert n == 1, "expected exactly 1 mmtk git dep, found %d" % n
open('Cargo.toml','w').write(out)
print("patched mmtk dep -> path = %s" % core)
PY

echo "=== $REV: oracle = $(git -C "$CORE" rev-parse --short HEAD) ($(git -C "$CORE" log -1 --format=%s)) ==="
echo "=== toolchain $TOOLCHAIN, features='${FEATURES}' ==="

# Keep the era-contemporaneous Cargo.lock: it pins crate versions that the era's
# rustc can actually build. Deleting it re-resolves to modern crates and fails.
git -C /root/lxr/$REV/mmtk-openjdk checkout -- mmtk/Cargo.lock 2>/dev/null && echo "restored Cargo.lock from git" || echo "no Cargo.lock tracked"

if [ -n "$FEATURES" ]; then
  cargo "+$TOOLCHAIN" check --release --features "$FEATURES" > /tmp/check-$REV.log 2>&1
else
  cargo "+$TOOLCHAIN" check --release > /tmp/check-$REV.log 2>&1
fi
CHECK_EXIT=$?
echo "--- errors (if any) ---"
grep -E '^(error|warning: unused)' /tmp/check-$REV.log | head -30 || true
echo "--- tail ---"
tail -8 /tmp/check-$REV.log
echo "CHECK_EXIT=$CHECK_EXIT  (full log: /tmp/check-$REV.log)"
