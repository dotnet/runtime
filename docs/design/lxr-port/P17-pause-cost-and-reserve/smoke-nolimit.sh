#!/bin/bash
# usage: smoke-nolimit.sh <frame-dir>...   one run of each workload at 4x in the nursery and mature modes, no hard limit
cd /root/p17s5b
for b in "$@"; do
  echo "== $b"
  for mode in "1 0" "1 1"; do
    for w in binarytrees:40 requests:84 listsuffix:40; do
      IFS=: read n h <<< "$w"
      start=$(date +%s.%N)
      out=$(bash run-nolimit.sh $b $n $h $mode | tail -1 | cut -c1-200)
      end=$(date +%s.%N)
      echo "mode=$mode $n $h: $(echo "$end - $start" | bc) s :: $out"
    done
  done
done
