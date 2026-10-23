#!/bin/bash
# P26: phases of the FinalMark pause of list suffix nursery in the traced runs of two steps. usage: finalmark26.sh <prefix> <step> ...
P=$1; shift
for s in "$@"; do
  echo "#### $s"
  python3 /root/p24/scripts/phases.py /root/p25/results/$P-$s-trace-listsuffix-lxr-nursery.txt | awk '/^== FinalMark/{f=1;next} /^== /{f=0} f' | grep -E 'stw_pause|prepare_trace|inc_wall|dec_wall|satb_wall|epoch_closure|nursery_sweep|retire_sources|publish|post_drain|pre_drain|weak|mature_sweep|release_epoch|prepare_work|traced_objects_d|inc_packets|dec_packets'
done
