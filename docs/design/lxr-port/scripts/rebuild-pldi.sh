#!/bin/bash
# P0.1 - rebuild PLDI with the CORRECT feature selection.
#
# Two bugs were found in the first attempt:
#   1. CompileThirdPartyHeap.gmk line 6 sets GC_FEATURES=--features $(MMTK_PLAN), and line 45
#      prepends another --features => "cargo build --features --features lxr". The feature never
#      took effect and the build silently ran NoGC. The correct knob is GC_FEATURES=<bare list>.
#   2. The binding's lib.rs forces MMTK_PLAN from *binding* features (nogc/semispace/.../immix).
#      There is no "lxr" case: the binding's lxr feature only forwards to mmtk/lxr, which
#      reconfigures the Immix plan. So the plan must ALSO be selected, via the `immix` feature.
# => GC_FEATURES=lxr,immix
set -uo pipefail
LVL=${1:?usage: rebuild-pldi.sh <release|fastdebug>}
BINDING=/root/lxr/pldi/mmtk-openjdk
export PATH=/root/.cargo/bin:$PATH
export LIBCLANG_PATH=/usr/lib/llvm-14/lib
export CARGO_NET_GIT_FETCH_WITH_CLI=true
unset MMTK_PLAN

cd "$BINDING/repos/openjdk" || exit 1
echo "=== rebuilding pldi/$LVL with GC_FEATURES=lxr,immix ==="
make CONF=linux-x86_64-normal-server-$LVL \
     THIRD_PARTY_HEAP=$BINDING/openjdk \
     GC_FEATURES=lxr,immix \
     > /root/lxr/logs/make-pldi-$LVL-lxr.log 2>&1
echo "MAKE_EXIT=$?"
grep -oE "cargo build[^\"]*" /root/lxr/logs/make-pldi-$LVL-lxr.log | sort -u | head -3
tail -3 /root/lxr/logs/make-pldi-$LVL-lxr.log
