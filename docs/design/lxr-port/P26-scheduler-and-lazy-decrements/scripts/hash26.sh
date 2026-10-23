#!/bin/bash
# P26: machine-code hash of the stock allocation helpers. libcoreclr.so is not rebuilt in P26 (all changes are in libclrlxrgc),
# so the hash is the one of the frame. The symbols come from the build output of the same libcoreclr.so.
D=/root/d2/main/artifacts/obj/coreclr/linux.x64.Release/dlls/mscoree/coreclr
cmp $D/libcoreclr.so /root/p25/frames/p26-final-Release-linux/libcoreclr.so && echo "frame libcoreclr.so is the build output"
cd /root/p25 && ./scripts/hash-helpers.sh $D/libcoreclr.so $D/libcoreclr.so.dbg 2>&1 | tail -4
