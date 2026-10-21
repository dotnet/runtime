#!/bin/bash
# Build the P17 tracer (GcTrace.cs) into /root/gcbench/trace18. The S4 tracer prints the two new phases resume_gap and
# publish_trace. Use it only with S4 builds: an older collector publishes fewer phases.
# usage: build-tracer.sh
S=/mnt/c/Users/konradkokosa/.copilot/repos/copilot-worktrees/runtime-fork/kkokosa-legendary-fiesta/docs/design/lxr-port/P17-pause-cost-and-reserve
D=/root/lxr-p5-1205f569/.dotnet/dotnet
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
rm -rf /root/p17s4/tracer-src /root/gcbench/trace18
mkdir -p /root/p17s4/tracer-src
sed 's/\r$//' $S/GcTrace.cs > /root/p17s4/tracer-src/GcTrace.cs
sed 's/\r$//' $S/GcTrace.csproj > /root/p17s4/tracer-src/GcTrace.csproj
cd /root/p17s4/tracer-src
$D build -c Release -o /root/gcbench/trace18 2>&1 | tail -3
cp /root/gcbench/trace17/gctrace.runtimeconfig.json /root/gcbench/trace18/gctrace.runtimeconfig.json
ls /root/gcbench/trace18
