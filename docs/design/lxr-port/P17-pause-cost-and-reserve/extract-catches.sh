#!/bin/bash
# Condense the gdb logs of the failure investigation (catch-fatal-s2.sh and catch-fail-site.sh) into two small text files.
# usage: extract-catches.sh <output-folder>
OUT=$1
R=/root/p17s2/results
{
  echo "# Watchpoint catches (catch-fail-site.sh): first write of the engine failure code. Build: S2 (frame s2j) and layer 1 (frame l1)."
  for d in catch-site-1 catch-site-2 catch-site; do
    for f in $R/$d/*.log; do
      grep -q 'Old value' $f || continue
      echo "== $d/$(basename $f)"
      awk '/Old value/{p=1} p{print substr($0,1,200)} /^\$2 =/{exit}' $f | grep -v -E '^   0x' | head -16
    done
  done
} > $OUT/failure-sites-watchpoint.txt
{
  echo "# Breakpoint catches (catch-fatal-s2.sh): message of the first fatal error and the engine state."
  for d in catch-fatal catch-fatal-s2; do
    for f in $R/$d/*.log; do
      grep -q 'Breakpoint 1,' $f || continue
      echo "== $d/$(basename $f)"
      grep -E '^0x[0-9a-f]+:|^\$[0-9]+ = ' $f | head -6
    done
  done
} > $OUT/failure-sites-fatal-message.txt
wc -l $OUT/failure-sites-*.txt
