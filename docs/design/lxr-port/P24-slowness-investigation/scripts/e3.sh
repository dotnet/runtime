#!/bin/bash
# E3: diagnostic variant. Skip the trace packet of an already marked (black) promoted object, concurrent trace left ON.
export FRAME=/root/p24/frames/p24diag-Release-linux
for i in 1 2; do
  for arm in lxr-nonmoving lxr-nursery; do
    /root/p24/scripts/cell.sh listsuffix $arm e3-ls-$arm-base-$i
    /root/p24/scripts/cell.sh listsuffix $arm e3-ls-$arm-skipblack-$i P24_SKIP_BLACK_TRACE=1
  done
done
for arm in lxr-nonmoving lxr-nursery; do
  /root/p24/scripts/cell.sh binarytrees $arm e3-bt-$arm-base-1
  /root/p24/scripts/cell.sh binarytrees $arm e3-bt-$arm-skipblack-1 P24_SKIP_BLACK_TRACE=1
  /root/p24/scripts/cell.sh requests $arm e3-rq-$arm-base-1
  /root/p24/scripts/cell.sh requests $arm e3-rq-$arm-skipblack-1 P24_SKIP_BLACK_TRACE=1
done
