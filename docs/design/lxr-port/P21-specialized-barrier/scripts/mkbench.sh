#!/bin/bash
# P21 (D1): build the store micro benchmark outside the repository folder.
set -eu
W=/mnt/c/Users/konradkokosa/.copilot/repos/copilot-worktrees/runtime-fork/kkokosa-expert-fortnight
S=/root/d1/storebench-src
rm -rf $S; mkdir -p $S
cp $W/docs/design/lxr-port/P21-specialized-barrier/StoreBench/* $S/
sed -i 's/\r$//' $S/*
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
cd /root/d1
/root/d1/main/.dotnet/dotnet build $S/StoreBench.csproj -c Release -o /root/d1/storebench 2>&1 | tail -3
