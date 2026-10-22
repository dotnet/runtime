#!/bin/bash
# E8: no hard limit (the engine may borrow to-space beyond the admitted heap, P17 S5b). Same initial capacity as before.
export P24_NOLIMIT=1
for arm in lxr-nonmoving lxr-defrag lxr-nursery lxr-mature; do
  /root/p24/scripts/cell.sh requests $arm e8-rq-$arm-nolimit
done
/root/p24/scripts/cell.sh listsuffix lxr-nonmoving e8-ls-lxr-nonmoving-nolimit
/root/p24/scripts/cell.sh binarytrees lxr-nonmoving e8-bt-lxr-nonmoving-nolimit
