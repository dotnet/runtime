#!/bin/bash
# Root capture failure: repeat the pin-heavy program in parallel loops (oversubscribed machine) and count failures.
# usage: rcf-stress.sh <frame> <runs per loop> <loops> <outdir> [nursery 0|1] [nopoh 0|1] [cores]
# Every failed run keeps its output in <outdir>/fail-<loop>-<run>.txt. The last line is "failures=<n> of <total>".
FRAME=$1; RUNS=${2:-10}; LOOPS=${3:-8}; OUT=$4; NUR=${5:-0}; NOPOH=${6:-0}; CORES=${7:-}
HERE=$(cd "$(dirname "$0")" && pwd)
mkdir -p "$OUT"
PROG=${PROG:-/root/gcbench/trace20/gctrace.dll}
for p in $(seq 1 $LOOPS); do
  (
  fail=0
  for i in $(seq 1 $RUNS); do
    if [ -n "$CORES" ]; then
      out=$(PROG=$PROG taskset -c $CORES bash $HERE/run-one.sh $FRAME pinheavy 64 $NUR 0 PINHEAVY_NOPOH=$NOPOH $EXTRA_ENV 2>&1)
    else
      out=$(PROG=$PROG bash $HERE/run-one.sh $FRAME pinheavy 64 $NUR 0 PINHEAVY_NOPOH=$NOPOH $EXTRA_ENV 2>&1)
    fi
    echo "$out" | grep -h 'LXRDIAG denied' >> "$OUT/denied-$p.txt"
    if ! echo "$out" | grep -q '"checksum"'; then
      fail=$((fail+1)); echo "$out" > "$OUT/fail-$p-$i.txt"
    fi
  done
  echo "$fail" > "$OUT/loop-$p.count"
  ) &
done
wait
total=$((RUNS * LOOPS)); sum=0
for p in $(seq 1 $LOOPS); do sum=$((sum + $(cat "$OUT/loop-$p.count"))); done
echo "failures=$sum of $total"
