#!/bin/bash
# P0 follow-up - run stock OpenJDK collectors with the P0.1 DaCapo/Hiccup method.
# usage: bench-stock.sh [iterations] [invocations] [heapMB...]
# TSV: collector bench heapMB invocation status msec <hiccup fields> exit wallSeconds
set -uo pipefail

ITERS=${1:-5}
INVOCS=${2:-3}
shift 2 || true
HEAPS="${*:-500 2000}"

echo core > /proc/sys/kernel/core_pattern 2>/dev/null
ulimit -c 0

JDK=/root/lxr/head/mmtk-openjdk/repos/openjdk/build/linux-x86_64-normal-server-release/jdk
JAVA="$JDK/bin/java"
DACAPO=/root/lxr/dacapo/dacapo-2006-10-MR2.jar
HARNESS=/root/lxr/harness/cls-head
OUT=/root/lxr/results
RESULT_NAME=${STOCK_RESULT_NAME:-final-stock-release}
TSV="$OUT/$RESULT_NAME.tsv"
LOGD="$OUT/flogs-$RESULT_NAME"
BENCHES=${BENCHES:-"antlr fop luindex lusearch pmd hsqldb eclipse xalan"}
COLLECTORS=${COLLECTORS:-"g1 parallel serial zgc"}
TMO=600

mkdir -p "$LOGD"
: > "$TSV"
printf 'collector\tbench\theapMB\tinvocation\tstatus\tmsec\tsamples\tmean\tp50\tp90\tp99\tp999\tmax\texit\twallSeconds\n' \
    >> "$TSV"

collector_flags() {
  case "$1" in
    g1)       printf '%s' '' ;;
    parallel) printf '%s' '-XX:+UseParallelGC' ;;
    serial)   printf '%s' '-XX:+UseSerialGC' ;;
    zgc)      printf '%s' '-XX:+UnlockExperimentalVMOptions -XX:+UseZGC' ;;
    *) return 2 ;;
  esac
}

run_one() {
  local collector=$1 bench=$2 heap=$3 inv=$4
  local flags
  flags=$(collector_flags "$collector") || return
  local log="$LOGD/$collector-$bench-$heap-$inv.log"

  cd /root/lxr/dacapo
  rm -f hs_err_pid*.log core.*
  local t0
  t0=$(date +%s)
  # shellcheck disable=SC2086 # flags is the intentionally tokenized collector option list.
  env -u MMTK_PLAN -u MMTK_VERBOSE timeout -k 10 "$TMO" \
      "$JAVA" $flags -server -XX:MetaspaceSize=100M \
      -Xms${heap}M -Xmx${heap}M \
      -Dbench.tag="$collector/$bench/$heap/$inv" -Ddacapo.main=Harness \
      -cp "$DACAPO:$HARNESS" Bench -n "$ITERS" "$bench" > "$log" 2>&1
  local rc=$?
  local elapsed=$(( $(date +%s) - t0 ))

  local status msec hic
  if grep -q "PASSED in" "$log"; then
    status=PASSED
    msec=$(grep "PASSED in" "$log" | tail -1 | sed -E 's/.*PASSED in ([0-9]+) msec.*/\1/')
  elif grep -qE "SIGSEGV|A fatal error|panicked at" "$log"; then
    status=CRASH
    msec=
  elif [ "$rc" -eq 124 ] || [ "$rc" -eq 137 ]; then
    status=TIMEOUT
    msec=
  else
    status=FAILED
    msec=
  fi
  if grep -qE "assert\(|Internal Error|guarantee\(|debug_assert" "$log"; then
    status="$status+ASSERT"
  fi
  hic=$(grep "^HICCUP" "$log" | tail -1 | sed -E 's/^HICCUP [^ ]+ //' | tr ' ' '\t')
  [ -z "$hic" ] && hic="-	-	-	-	-	-	-"
  printf '%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\n' \
      "$collector" "$bench" "$heap" "$inv" "$status" "${msec:--}" \
      "$hic" "$rc" "$elapsed" >> "$TSV"
  echo "  $collector/$bench heap=${heap}M inv=$inv -> $status ${msec:-}ms (${elapsed}s)"
}

echo "===== stock collectors | release | -n $ITERS | invocations=$INVOCS | heaps: $HEAPS ====="
echo "JDK: $("$JAVA" -version 2>&1 | head -1)"
echo "Java SHA-256: $(sha256sum "$JAVA" | cut -d' ' -f1)"
echo "DaCapo SHA-256: $(sha256sum "$DACAPO" | cut -d' ' -f1)"
for heap in $HEAPS; do
  for collector in $COLLECTORS; do
    for bench in $BENCHES; do
      for inv in $(seq 1 "$INVOCS"); do
        run_one "$collector" "$bench" "$heap" "$inv"
      done
    done
  done
done
echo "===== done -> $TSV ====="
