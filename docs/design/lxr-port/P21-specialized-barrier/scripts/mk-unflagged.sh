#!/bin/bash
# P21 (D1): make a ReadyToRun CoreLib WITHOUT --old-value-write-barriers (for the negative control of the R2R cell).
set -eu
B=/root/d1/main/artifacts/bin/coreclr/linux.x64.Release
mkdir -p /root/d1/unflagged
/root/d1/main/.dotnet/dotnet $B/crossgen2/crossgen2.dll $B/IL/System.Private.CoreLib.dll -o /root/d1/unflagged/System.Private.CoreLib.dll \
  -r "$B/IL/*.dll" --targetos:linux --targetarch:x64 -O --target-allows-runtime-code-generation:true \
  --target-single-threaded:false --verify-type-and-field-layout --enable-cached-interface-dispatch-support --strip-inlining-info --strip-debug-info 2>&1 | tail -5
ls -la /root/d1/unflagged/
