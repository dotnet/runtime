#!/bin/bash
# pin-heavy serial extra runs (two repetitions, interleaved), traced, into /root/s7/trace-ph
OUT=/root/s7/trace-ph
mkdir -p $OUT
EXTRA="stamp_ns=243,native_planes=38,gc_committed=26,pinned_roots=458,pins_inplace=81"
for rep in 0 1; do
  for m in 0:0 1:0; do
    IFS=: read n mm <<< "$m"
    order="int s7"; [ $rep -eq 1 ] && order="s7 int"
    for b in $order; do
      case $b in int) FRAME=/root/int/frames/int-Release-linux;; s7) FRAME=/root/s7/frames/s7-Release-linux;; esac
      PROG=/root/gcbench/trace20/gctrace.dll taskset -c 0-4 bash /root/s7/tree/docs/design/lxr-port/P17-pause-cost-and-reserve/run-one.sh $FRAME pinheavy 64 $n $mm GCTRACE=1 GCTRACE_EXTRA="$EXTRA" > $OUT/$b-pinheavy-64-$n$mm-r$rep.txt 2>&1
    done
  done
done
ls $OUT
