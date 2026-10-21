#!/bin/bash
# P0.1 - final DaCapo 2006 pass on both reference revisions.
#   usage: bench-final.sh <release|fastdebug> <iterations> <invocations> <heapMB...>
# TSV: rev level bench heapMB invocation status msec <hiccup fields>
#
# Heap sizes are FIXED, not derived from minheaps: the reference's own
# ci-test-normal.sh runs every DaCapo 2006 benchmark at -Xms500M -Xmx500M and
# notes those options "are necessary for now to ensure the benchmarks work".
#
# Plan selection:
#   pldi - compile-time, GC_FEATURES=lxr,immix (see P0.1-reference-build.md 5.1/5.2)
#   head - runtime, MMTK_PLAN=LXR
set -uo pipefail
LVL=${1:-release}
ITERS=${2:-5}
INVOCS=${3:-3}
shift 3 || true
HEAPS="${*:-500 2000}"

# Stop WSL's crash-capture pipe from turning every SIGSEGV into a ~150s stall.
echo core > /proc/sys/kernel/core_pattern 2>/dev/null
ulimit -c 0

DACAPO=/root/lxr/dacapo/dacapo-2006-10-MR2.jar
OUT=/root/lxr/results
TSV="$OUT/final-$LVL.tsv"
LOGD="$OUT/flogs-$LVL"
mkdir -p "$LOGD"
: > "$TSV"
BENCHES="antlr fop luindex lusearch pmd hsqldb eclipse xalan"
TMO=600
[ "$LVL" = fastdebug ] && TMO=1200

run_one() {
  local rev=$1 bench=$2 heap=$3 inv=$4
  local jdk=/root/lxr/$rev/mmtk-openjdk/repos/openjdk/build/linux-x86_64-normal-server-$LVL/jdk/bin/java
  local log="$LOGD/$rev-$bench-$heap-$inv.log"
  local pre=""
  [ "$rev" = head ] && pre="MMTK_PLAN=LXR"

  cd /root/lxr/dacapo
  rm -f hs_err_pid*.log core.*
  local t0=$(date +%s)
  env $pre timeout -k 10 $TMO "$jdk" -XX:+UseThirdPartyHeap -server -XX:MetaspaceSize=100M \
      -Xms${heap}M -Xmx${heap}M \
      -Dbench.tag="$rev/$bench/$heap/$inv" -Ddacapo.main=Harness \
      -cp "$DACAPO:/root/lxr/harness/cls-$rev" Bench -n "$ITERS" "$bench" > "$log" 2>&1
  local rc=$? el=$(( $(date +%s) - t0 ))

  local status msec hic
  if grep -q "PASSED in" "$log"; then
    status=PASSED
    msec=$(grep "PASSED in" "$log" | tail -1 | sed -E 's/.*PASSED in ([0-9]+) msec.*/\1/')
  elif grep -qE "SIGSEGV|A fatal error|panicked at" "$log"; then
    status=CRASH; msec=
  elif [ $rc -eq 124 ] || [ $rc -eq 137 ]; then
    status=TIMEOUT; msec=
  else
    status=FAILED; msec=
  fi
  # assertion failures are what the fastdebug pass exists to detect
  if grep -qE "assert\(|Internal Error|guarantee\(|debug_assert" "$log"; then
    status="$status+ASSERT"
  fi
  hic=$(grep "^HICCUP" "$log" | tail -1 | sed -E 's/^HICCUP [^ ]+ //' | tr ' ' '\t')
  [ -z "$hic" ] && hic="-"
  printf '%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\n' \
      "$rev" "$LVL" "$bench" "$heap" "$inv" "$status" "${msec:--}" "$hic" >> "$TSV"
  echo "  $rev/$bench heap=${heap}M inv=$inv -> $status ${msec:-}ms (${el}s)"
}

echo "===== DaCapo 2006 | $LVL | -n $ITERS | invocations=$INVOCS | heaps: $HEAPS ====="
for heap in $HEAPS; do
  for rev in pldi head; do
    for b in $BENCHES; do
      for inv in $(seq 1 "$INVOCS"); do
        run_one "$rev" "$b" "$heap" "$inv"
      done
    done
  done
done
echo "===== done -> $TSV ====="
