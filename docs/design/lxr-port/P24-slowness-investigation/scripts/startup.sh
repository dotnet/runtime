#!/bin/bash
# Start-up cost: a tiny run (binary trees depth 6: no collection is needed) on each collector, 7 runs each, median wall time.
export P24_ARGS="6 0"
for arm in wks lxr-nonmoving lxr-nursery; do
  for i in 1 2 3 4 5 6 7; do
    /root/p24/scripts/run1.sh /root/p24/frames/p24-Release-linux /root/p24/tracer-bin/gctrace.dll binarytrees $arm | grep wall_s
  done | sort | awk -v a=$arm '{split($1,x,"="); v[NR]=x[2]} END {print a, "median wall_s", v[4], "min", v[1], "max", v[7]}'
done
export P24_ARGS="6 0"
echo "ReadyToRun off:"
for arm in wks lxr-nonmoving; do
  for i in 1 2 3 4 5; do
    /root/p24/scripts/run1.sh /root/p24/frames/p24-Release-linux /root/p24/tracer-bin/gctrace.dll binarytrees $arm DOTNET_ReadyToRun=0 | grep wall_s
  done | sort | awk -v a=$arm '{split($1,x,"="); v[NR]=x[2]} END {print a, "median wall_s", v[3], "min", v[1], "max", v[5]}'
done
