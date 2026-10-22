#!/bin/bash
# P22 (D2): build the managed fixture (docs/design/lxr-port/P5/runtime-tests) for Release and Debug.
set -eu
. "$(dirname "$0")/env.sh"
S=$D/fixsrc
rm -rf $S; mkdir -p $S
cp $W/docs/design/lxr-port/P5/runtime-tests/*.cs $W/docs/design/lxr-port/P5/runtime-tests/runtime-tests.csproj $S/
sed -i 's/\r$//' $S/*.cs $S/*.csproj
for C in Release Debug; do
  $T/.dotnet/dotnet build $S/runtime-tests.csproj -c $C -o $D/fixtures/fixture-scope5-$C 2>&1 | tail -4
done
