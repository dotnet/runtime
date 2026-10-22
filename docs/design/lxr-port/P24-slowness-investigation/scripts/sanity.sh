#!/bin/bash
# Sanity run: the p24 frame against the d2 frame, listsuffix, one after the other (each 20-35 s).
cd /root/p24
for f in p24 d2; do
  if [ $f = p24 ]; then F=/root/p24/frames/p24-Release-linux; else F=/root/d2/frames/d2-Release-linux; fi
  for arm in lxr-nursery; do
    echo "== $f $arm"
    /root/p24/scripts/run1.sh $F /root/gcbench/net/gcbench.dll listsuffix $arm | tail -2
  done
done
