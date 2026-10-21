#!/bin/bash
# Make a flat runtime directory (a "frame") from a Linux build tree: runtime binaries, libraries, corerun and the
# LXR collector. A frame is what DOTNET_GCPath and corerun are used with in the demo and benchmark commands.
# usage: mkframe.sh <repo-root> <Release|Debug> <label> [frames-root]
set -eu
R=$1; C=$2; L=$3
ROOT=${4:-$HOME/lxr-frames}
F=$ROOT/$L-$C-linux
rm -rf "$F"; mkdir -p "$F"
CO=$R/artifacts/bin/coreclr/linux.x64.$C
cp -a "$R/artifacts/bin/runtime/net11.0-linux-$C-x64/." "$F/"
find "$CO" -maxdepth 1 -type f \( -name '*.so' -o -name '*.dll' -o -name corerun -o -name '*.json' \) -exec cp -f {} "$F/" \;
cp -f "$CO/IL/System.Private.CoreLib.dll" "$F/System.Private.CoreLib.dll"
ls "$F" | wc -l
ls -la "$F/corerun" "$F/libcoreclr.so" "$F/libclrjit.so" "$F/libclrlxrgc.so" "$F/System.Private.CoreLib.dll"
