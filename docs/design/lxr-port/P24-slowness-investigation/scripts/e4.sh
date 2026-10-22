#!/bin/bash
# E4: number of collector workers (diagnostic variant, P24_WORKERS). Concurrent trace off (latency batch) so the effect of the
# worker count is not hidden by the trace backlog; the P24_SKIP_BLACK_TRACE=1 column keeps the default trace on.
export FRAME=/root/p24/frames/p24diag-Release-linux
for n in 1 2 4 8 12; do
  /root/p24/scripts/cell.sh listsuffix lxr-nonmoving e4-ls-nonmoving-batch-w$n GCBENCH_LATENCY=batch P24_WORKERS=$n
  /root/p24/scripts/cell.sh listsuffix lxr-nonmoving e4-ls-nonmoving-skipblack-w$n P24_SKIP_BLACK_TRACE=1 P24_WORKERS=$n
  /root/p24/scripts/cell.sh requests lxr-nursery e4-rq-nursery-w$n P24_WORKERS=$n
  /root/p24/scripts/cell.sh binarytrees lxr-nursery e4-bt-nursery-batch-w$n GCBENCH_LATENCY=batch P24_WORKERS=$n
done
