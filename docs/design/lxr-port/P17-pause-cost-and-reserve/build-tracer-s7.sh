#!/bin/bash
# Build the P17 tracer (GcTrace.cs, with the S7 workload "pinheavy") into /root/gcbench/trace20.
# usage: build-tracer-s7.sh <P17-folder-in-WSL>
S=${1:-/root/s7/tree/docs/design/lxr-port/P17-pause-cost-and-reserve}
D=/root/int/main/.dotnet/dotnet
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
rm -rf /root/s7/tracer-src /root/gcbench/trace20
mkdir -p /root/s7/tracer-src
sed 's/\r$//' $S/GcTrace.cs > /root/s7/tracer-src/GcTrace.cs
sed 's/\r$//' $S/GcTrace.csproj > /root/s7/tracer-src/GcTrace.csproj
cd /root/s7/tracer-src
$D build -c Release -o /root/gcbench/trace20 2>&1 | tail -3
cp /root/gcbench/trace19/gctrace.runtimeconfig.json /root/gcbench/trace20/gctrace.runtimeconfig.json
ls /root/gcbench/trace20
