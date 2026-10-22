#!/bin/bash
# P25: phase medians of the RefCount pauses of the traced runs (phases.py of P24). usage: phases-final.sh <results-dir> <tag>
R=$1; T=$2
for c in listsuffix-lxr-nursery listsuffix-lxr-nonmoving requests-lxr-nursery requests-lxr-nonmoving binarytrees-lxr-nursery; do
  echo "== $T $c"
  python3 /root/p24/scripts/phases.py $R/$T-trace-$c.txt | awk '/^== RefCount/{f=1} /^== (InitialMark|FinalMark|Full)/{f=0} f' | grep -E 'RefCount|stw_pause|prepare_trace_wall|inc_wall|dec_wall|post_drain|publish_trace|suspend_prepare|^incs_d|inc_packets|dec_packets|lock_wait'
done
