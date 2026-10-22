#!/bin/bash
# P21 (D1): build the managed fixture (docs/design/lxr-port/P5/runtime-tests) for Release and Debug.
set -eu
W=/mnt/c/Users/konradkokosa/.copilot/repos/copilot-worktrees/runtime-fork/kkokosa-expert-fortnight
S=/root/d1/fixsrc
rm -rf $S; mkdir -p $S
cp $W/docs/design/lxr-port/P5/runtime-tests/*.cs $W/docs/design/lxr-port/P5/runtime-tests/runtime-tests.csproj $S/
sed -i 's/\r$//' $S/*.cs $S/*.csproj
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
for C in Release Debug; do
  /root/d1/main/.dotnet/dotnet build $S/runtime-tests.csproj -c $C -o /root/d1/fixtures/fixture-scope5-$C 2>&1 | tail -4
done
