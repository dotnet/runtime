#!/bin/bash
# P17 S7 lean benchmark: the integrated tip against the S7 tip, heap x4, three workloads, modes non-moving and nursery
# copy, two repetitions in two parallel streams on pinned cores, builds interleaved. Traced runs (pauses from the S0
# timers, native planes, committed bytes) and the pin-heavy program use the same tracer.
# usage: bench-s7.sh <P17-scripts-dir> <out-dir> <int-frame> <s7-frame> [tracer-dll]
set -u
P17=$1; OUT=$2; INT=$3; S7=$4; TRACER=${5:-/root/gcbench/trace20/gctrace.dll}
mkdir -p "$OUT"
CASES="binarytrees:40 requests:84 listsuffix:40 pinheavy:64"
MODES="0:0 1:0"
EXTRA="stamp_ns=243,native_planes=38,gc_committed=26,pinned_roots=458,pins_inplace=81"
stream() {
  rep=$1
  for c in $CASES; do
    IFS=: read w h <<< "$c"
    for m in $MODES; do
      IFS=: read n mm <<< "$m"
      order="int s7"
      [ $((rep % 2)) -eq 1 ] && order="s7 int"
      for b in $order; do
        case $b in
          int) FRAME=$INT;;
          s7) FRAME=$S7;;
        esac
        PROG=$TRACER bash "$P17/run-one.sh" "$FRAME" $w $h $n $mm GCTRACE=1 GCTRACE_EXTRA="$EXTRA" > "$OUT/$b-$w-$h-$n$mm-r$rep.txt" 2>&1
      done
    done
  done
}
taskset -c 0-4 bash -c "$(declare -f stream); P17=$P17 OUT=$OUT INT=$INT S7=$S7 TRACER=$TRACER CASES='$CASES' MODES='$MODES' EXTRA='$EXTRA'; stream 0" &
taskset -c 5-9 bash -c "$(declare -f stream); P17=$P17 OUT=$OUT INT=$INT S7=$S7 TRACER=$TRACER CASES='$CASES' MODES='$MODES' EXTRA='$EXTRA'; stream 1" &
wait
echo "S7 runs finished: $(ls "$OUT" | wc -l) files"
