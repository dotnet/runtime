#!/bin/bash
# Build the P24 variant of the tracer (GcTrace.cs: P17 tracer + GCBENCH_LATENCY=batch + per-thread CPU in the summary).
# Output: /root/p24/tracer-bin/gctrace.dll
D=/root/lxr-p5-1205f569/.dotnet/dotnet
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
rm -rf /root/p24/tracer-src /root/p24/tracer-bin
mkdir -p /root/p24/tracer-src
for f in GcTrace.cs GcTrace.csproj; do tr -d '\r' < /root/p24/scripts/tracer/$f > /root/p24/tracer-src/$f; done
cd /root/p24/tracer-src
$D build -c Release -o /root/p24/tracer-bin 2>&1 | tail -3
cp /root/gcbench/trace19/gctrace.runtimeconfig.json /root/p24/tracer-bin/gctrace.runtimeconfig.json
ls /root/p24/tracer-bin | head
