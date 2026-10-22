#!/bin/bash
# P21 (D1): make a flat runtime directory (a frame) that uses the ReadyToRun CoreLib compiled with
# --old-value-write-barriers (the default CoreLib of the product build). With the argument "il" the IL CoreLib is used.
# usage: mkframe-r2r.sh <repo-in-wsl> <Release|Debug> <label> [il|r2r|unflagged <path-to-unflagged-corelib>]
set -eu
R=$1; C=$2; L=$3; MODE=${4:-r2r}
F=/root/d1/frames/$L-$C-linux
rm -rf "$F"; mkdir -p "$F"
CO=$R/artifacts/bin/coreclr/linux.x64.$C
cp -a "$R/artifacts/bin/runtime/net11.0-linux-$C-x64/." "$F/" 2>/dev/null || true
find "$CO" -maxdepth 1 -type f \( -name '*.so' -o -name '*.dll' -o -name corerun -o -name '*.json' \) -exec cp -f {} "$F/" \;
case "$MODE" in
  il) cp -f "$CO/IL/System.Private.CoreLib.dll" "$F/System.Private.CoreLib.dll" ;;
  unflagged) cp -f "$5" "$F/System.Private.CoreLib.dll" ;;
  r2r) ;;
esac
ls "$F" | wc -l
ls -la "$F/corerun" "$F/libcoreclr.so" "$F/libclrjit.so" "$F/libclrlxrgc.so" "$F/System.Private.CoreLib.dll"
