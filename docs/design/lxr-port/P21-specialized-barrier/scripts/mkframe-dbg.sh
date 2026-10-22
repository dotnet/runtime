#!/bin/bash
# P21 (D1): make Debug frames from the D1 build tree.
#   d1-Release-linux (exists) + Debug collector            -> d1dbgcol-Release-linux  (managed cells with the Debug collector)
#   d1-Release-linux + Debug libcoreclr/libclrjit/CoreLib  -> d1dbgrt-Release-linux   (full Debug runtime, asserts on)
set -eu
R=/root/d1/main
D=$R/artifacts/bin/coreclr/linux.x64.Debug
F=/root/d1/frames
rm -rf $F/d1dbgcol-Release-linux $F/d1dbgrt-Release-linux
cp -al $F/d1-Release-linux $F/d1dbgcol-Release-linux
cp --remove-destination $D/libclrlxrgc.so $F/d1dbgcol-Release-linux/libclrlxrgc.so
cp -al $F/d1-Release-linux $F/d1dbgrt-Release-linux
for f in libclrlxrgc.so libcoreclr.so libclrjit.so System.Private.CoreLib.dll; do
  cp --remove-destination $D/$f $F/d1dbgrt-Release-linux/$f
done
ls -la $F/d1dbgrt-Release-linux/libcoreclr.so $F/d1dbgrt-Release-linux/libclrjit.so $F/d1dbgrt-Release-linux/System.Private.CoreLib.dll $F/d1dbgcol-Release-linux/libclrlxrgc.so
