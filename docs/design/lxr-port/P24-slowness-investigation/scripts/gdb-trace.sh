#!/bin/bash
# Start a slow-mode list suffix run, attach gdb after a delay, print the trace state, then kill the run.
# usage: gdb-trace.sh <delay-seconds> <out-file>
DELAY=${1:-12}; OUT=${2:-/root/p24/results/gdb-trace-state.txt}
/root/p24/scripts/run1.sh /root/p24/frames/p24-Release-linux /root/p24/tracer-bin/gctrace.dll listsuffix lxr-nursery > /root/p24/results/gdb-run.txt 2>&1 &
sleep $DELAY
PID=$(pgrep -x corerun | head -1)
echo "pid=$PID" > $OUT
env -i PATH=/usr/bin:/bin timeout 300 gdb -batch -x /root/p24/scripts/trace-state.py -p $PID >> $OUT 2>&1
kill $PID 2>/dev/null
sleep 1
tail -40 $OUT
