#!/bin/bash
# The S6 finalization-delay measurements: accepted build, S5b tip and S6 build, natural collections, hard limit.
# usage: lag-matrix.sh <s5b-frame> <s6-frame> [repetitions] [output]
S5=$1; S6=$2; REPS=${3:-3}; OUT=${4:-/root/p17/results/lag-s6.jsonl}
ACC=/root/lxr-batching-d2eccaea/after-Release-linux
: ${ONLY:+}; [ -z "${ONLY:-}" ] && : > $OUT
# config name: objects ring payload disposeEvery threads heap plain
CONFIGS=(
  "small:100000:1000:64:0:1:256:"
  "all-disposed:2000000:50000:64:1:1:256:"
  "churn:2000000:50000:64:0:1:256:"
  "half-disposed:2000000:50000:64:2:1:256:"
  "churn-4-threads:2000000:50000:64:0:4:256:"
  "control-no-finalizer:2000000:50000:64:0:1:256:plain"
  "pressure:120000:1000:4096:0:1:256:"
  "pressure-control:120000:1000:4096:0:1:256:plain"
)
for rep in $(seq 1 $REPS); do
  for cfg in "${CONFIGS[@]}"; do
    IFS=: read name obj ring pay disp thr heap plain <<< "$cfg"
    [ -n "${ONLY:-}" ] && [[ " $ONLY " != *" $name "* ]] && continue
    for mode in nonmoving nursery mature; do
      order="acc s5b s6"; [ $((rep % 2)) -eq 0 ] && order="s6 s5b acc"
      for b in $order; do
        case $b in acc) F=$ACC;; s5b) F=$S5;; s6) F=$S6;; esac
        line=$(TAILN=1 TMO=300 bash /root/p17/lag1.sh $F $mode $obj $ring $pay $disp $thr $heap $plain 2>&1 | grep -E '^\{' | head -1)
        if [ -z "$line" ]; then line="{\"failed\":true}"; fi
        echo "{\"config\":\"$name\",\"build\":\"$b\",\"mode\":\"$mode\",\"rep\":$rep,\"result\":$line}" >> $OUT
      done
    done
  done
  echo "rep $rep done"
done
