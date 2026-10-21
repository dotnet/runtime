#!/bin/bash
# P0.3 probe 1 - run the instrumented lxr-head oracle under load.
#
# Question: does the lxr-head oracle uphold PLDI's barrier invariant
#   old.is_null() || rc::count(old) != 0
# under the same load that trips it at the PLDI oracle?  P0.1 could not have
# answered this: the shipped head fastdebug .so contains no such assertion
# (`strings ... | grep -c 'zero rc count'` == 0), so its silence was vacuous.
#
# Mirrors scripts/bench-final.sh so numbers are comparable with P0.1:
# DaCapo 2006, -n 5, fastdebug, MMTK_PLAN=LXR, fixed heaps.
#
# Restores the shipped head fastdebug .so on exit - the reference is left as found.
set -uo pipefail
ITERS=${1:-5}
INVOCS=${2:-3}
shift 2 || true
HEAPS="${*:-2000 500}"

JDKDIR=/root/lxr/head/mmtk-openjdk/repos/openjdk/build/linux-x86_64-normal-server-fastdebug
SO=$JDKDIR/jdk/lib/server/libmmtk_openjdk.so
DACAPO=/root/lxr/dacapo/dacapo-2006-10-MR2.jar
OUT=/root/lxr/results
TSV="$OUT/probe1-fastdebug.tsv"
LOGD="$OUT/flogs-probe1"
BENCHES="antlr fop luindex lusearch pmd hsqldb eclipse xalan"
TMO=1200

restore () {
  echo "-- restoring shipped head fastdebug .so --"
  cp -a /root/lxr/so-backup/head-fastdebug-shipped.so "$SO" \
    && echo "   restored; 'zero rc count' occurrences now: $(strings "$SO" | grep -c 'zero rc count')"
}
trap restore EXIT

echo core > /proc/sys/kernel/core_pattern 2>/dev/null
ulimit -c 0
mkdir -p "$LOGD"
: > "$TSV"

echo "########## 0. confirm the INSTRUMENTED .so is installed ##########"
cp -a /root/lxr/so-backup/head-fastdebug-probe1.so "$SO" || exit 1
n=$(strings "$SO" | grep -c 'zero rc count')
echo "'zero rc count' occurrences in installed .so: $n"
[ "$n" -ge 1 ] || { echo "ABORT: instrumented .so not installed"; exit 1; }

run_one() {
  local bench=$1 heap=$2 inv=$3
  local log="$LOGD/probe1-$bench-$heap-$inv.log"
  cd /root/lxr/dacapo
  rm -f hs_err_pid*.log core.*
  local t0=$(date +%s)
  env MMTK_PLAN=LXR timeout -k 10 $TMO "$JDKDIR/jdk/bin/java" \
      -XX:+UseThirdPartyHeap -server -XX:MetaspaceSize=100M \
      -Xms${heap}M -Xmx${heap}M \
      -Dbench.tag="probe1/$bench/$heap/$inv" -Ddacapo.main=Harness \
      -cp "$DACAPO:/root/lxr/harness/cls-head" Bench -n "$ITERS" "$bench" > "$log" 2>&1
  local rc=$? el=$(( $(date +%s) - t0 ))

  local status msec
  if grep -q "PASSED in" "$log"; then
    status=PASSED; msec=$(grep "PASSED in" "$log" | tail -1 | sed -E 's/.*PASSED in ([0-9]+) msec.*/\1/')
  elif grep -qE "SIGSEGV|A fatal error|panicked at" "$log"; then status=CRASH; msec=
  elif [ $rc -eq 124 ] || [ $rc -eq 137 ]; then status=TIMEOUT; msec=
  else status=FAILED; msec=; fi
  # THE question this probe exists to answer
  local zrc=no
  grep -q "zero rc count" "$log" && { zrc=YES; status="$status+ZERORC"; }
  grep -qE "assert\(|Internal Error|guarantee\(|debug_assert|panicked at" "$log" && status="$status+ASSERT"

  local hic=$(grep "^HICCUP" "$log" | tail -1 | sed -E 's/^HICCUP [^ ]+ //' | tr ' ' '\t')
  [ -z "$hic" ] && hic="-"
  printf 'probe1\tfastdebug\t%s\t%s\t%s\t%s\t%s\t%s\t%s\n' \
      "$bench" "$heap" "$inv" "$status" "${msec:--}" "$zrc" "$hic" >> "$TSV"
  echo "  probe1/$bench heap=${heap}M inv=$inv -> $status ${msec:-}ms (${el}s) zero_rc=$zrc"
}

echo
echo "===== DaCapo 2006 | probe1 fastdebug | -n $ITERS | invocations=$INVOCS | heaps: $HEAPS ====="
for heap in $HEAPS; do
  for b in $BENCHES; do
    for inv in $(seq 1 "$INVOCS"); do run_one "$b" "$heap" "$inv"; done
  done
done

echo
echo "########## SCAN EVERY LOG (P0.1 7.3: scanning one dir of seven gave a wrong answer once) ##########"
echo "-- logs mentioning 'zero rc count' --"
grep -l "zero rc count" "$LOGD"/*.log 2>/dev/null | sed 's|.*/||' || true
echo "   count: $(grep -l 'zero rc count' "$LOGD"/*.log 2>/dev/null | wc -l) of $(ls "$LOGD"/*.log 2>/dev/null | wc -l)"
echo "-- first zero-rc site, if any --"
grep -m1 -A4 "zero rc count" "$LOGD"/*.log 2>/dev/null | head -30 || true
echo "-- any other rust panic --"
grep -h -m1 "panicked at" "$LOGD"/*.log 2>/dev/null | sort -u | head -10 || true
echo "-- status tally --"
cut -f6 "$TSV" | sort | uniq -c | sort -rn
echo "probe1-run done -> $TSV"
