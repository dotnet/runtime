#!/bin/bash
# P28: build the managed fixture (docs/design/lxr-port/P5/runtime-tests) from the worktree for Release and Debug.
# usage: mkfixtures28.sh <worktree-wsl-path>        output: /root/p28/fixtures/fixture-scope5-{Release,Debug}
set -eu
W=$1
S=/root/p28/fixsrc
rm -rf $S; mkdir -p $S
cp $W/docs/design/lxr-port/P5/runtime-tests/*.cs $W/docs/design/lxr-port/P5/runtime-tests/runtime-tests.csproj $S/
sed -i 's/\r$//' $S/*.cs $S/*.csproj
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
for C in Release Debug; do
  /root/d2/main/.dotnet/dotnet build $S/runtime-tests.csproj -c $C -o /root/p28/fixtures/fixture-scope5-$C 2>&1 | tail -3
done
