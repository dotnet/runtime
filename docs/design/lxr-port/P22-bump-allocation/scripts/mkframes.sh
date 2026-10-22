#!/bin/bash
# P22 (D2): make the frames (flat runtime directories) from the build tree.
#   d2-Release-linux     Release runtime and collector (ReadyToRun CoreLib with --old-value-write-barriers)
#   d2dbgcol-Release-linux  the same with the Debug collector (managed cells with the Debug collector)
#   d2dbgrt-Release-linux   a full Debug runtime (asserts on)
set -eu
. "$(dirname "$0")/env.sh"
F=$D/frames
mkdir -p $F
mkframe() { # label config
  local L=$1 C=$2
  local FR=$F/$L-linux
  rm -rf "$FR"; mkdir -p "$FR"
  local CO=$T/artifacts/bin/coreclr/linux.x64.$C
  cp -a "$T/artifacts/bin/runtime/net11.0-linux-$C-x64/." "$FR/" 2>/dev/null || true
  find "$CO" -maxdepth 1 -type f \( -name '*.so' -o -name '*.dll' -o -name corerun -o -name '*.json' \) -exec cp -f {} "$FR/" \;
}
mkframe d2-Release Release
D2DBG=$T/artifacts/bin/coreclr/linux.x64.Debug
if [ -d "$D2DBG" ]; then
  rm -rf $F/d2dbgcol-Release-linux $F/d2dbgrt-Release-linux
  cp -al $F/d2-Release-linux $F/d2dbgcol-Release-linux
  cp --remove-destination $D2DBG/libclrlxrgc.so $F/d2dbgcol-Release-linux/libclrlxrgc.so
  cp -al $F/d2-Release-linux $F/d2dbgrt-Release-linux
  for f in libclrlxrgc.so libcoreclr.so libclrjit.so System.Private.CoreLib.dll; do
    cp --remove-destination $D2DBG/$f $F/d2dbgrt-Release-linux/$f
  done
fi
ls $F
