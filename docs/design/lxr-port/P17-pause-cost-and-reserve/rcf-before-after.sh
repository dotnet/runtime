#!/bin/bash
# Root capture failure: before and after counts. Runs the oversubscribed pin-heavy loop for each arm, interleaved.
# usage: rcf-before-after.sh <unfixed-frame> <fixed-frame> <rounds> <outdir>
# One round is 8 parallel loops of 13 runs (104 runs) for each of four arms: unfixed and fixed, with and without
# pinned-object-heap arrays. The result lines go to <outdir>/summary.txt.
UNFIXED=$1; FIXED=$2; ROUNDS=${3:-1}; OUT=$4
HERE=$(cd "$(dirname "$0")" && pwd)
mkdir -p "$OUT"
for r in $(seq 1 $ROUNDS); do
  for arm in unfixed-poh fixed-poh unfixed-nopoh fixed-nopoh; do
    case $arm in
      unfixed-*) FRAME=$UNFIXED;;
      *) FRAME=$FIXED;;
    esac
    case $arm in
      *-nopoh) NOPOH=1;;
      *) NOPOH=0;;
    esac
    line=$(bash "$HERE/rcf-stress.sh" "$FRAME" 13 8 "$OUT/$arm-r$r" 0 $NOPOH | tail -1)
    echo "round $r $arm $line" | tee -a "$OUT/summary.txt"
  done
done
