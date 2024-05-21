# Test Results

This folder has the results of manual testing observed for this feature. It is here so reviewers can see it but is planned to be deleted before the PR is merged.

## statistical distribution measures
The manual folder contains code to allocate and count objects in different runs.

# Perf benchmarking
The performance impact of the PR has been measured against a baseline.
Each branch is built on Windows for x64 with:
   .\build.cmd -s clr+libs -c release
   src\tests\build.cmd generatelayoutonly Release

## Baseline
commit d1f0e2930f86e8771ccbefa96aead6f960ecc3f4 (HEAD)
Author: Stephen Toub <stoub@microsoft.com>
Date:   Sat Feb 3 18:52:31 2024 -0500

This is what is used for all "Baseline" measurements because the changes in this PR started from here.

## PR
Latest version of the modified CoreCLR

## Tool
The GCPerfSim module from the Performance repository has been run 10 times to allocate 500 GB of mixed size objects on 4 threads with a 50MB live object size.
<path_to_repo>\artifacts\tests\coreclr\windows.x64.Release\Tests\Core_Root\corerun.exe C:\git\benchmarks\artifacts\bin\GCPerfSim\release\net7.0\GCPerfSim.dll -tc 4 -tagb 500 -tlgb 0.05 -lohar 0 -sohsi 0 -lohsi 0 -pohsi 0 -sohpi 0 -lohpi 0 -sohfi 0 -lohfi 0 -pohfi 0 -allocType reference -testKind time

Here is the command line to measure the impact of computed and emitted events:
dotnet-trace collect --show-child-io true --providers Microsoft-Windows-DotNETRuntime:0x1:5 -- <same as previous command line>

The goal is to emphasize the impact of allocations on performance and GC collection overhead.

## Results
The two implementation are very close in terms of impact.
- GCPerfSimx10_Baseline.txt: .NET version before the PR
  19.6675793   (median)
  19.82903766  (average)

- GCPerfSimx10_PullRequest.txt: PR without provider enabled
  19.7984609  (median)
  19.7717041  (average)

It is expected that AllocationTick is more expensive because of the required Verbosity level that emits much more events than just AllocationTick:
- GCPerfSimx10_PullRequest+Events.txt: same but with AllocationSampled emitted
  21.0216025  (median)
  21.03864168 (average)

- GCPerfSimx10_Baseline+AllocationTick.txt: same but with AllocationTick emitted
  22.6581132  (median)
  22.78253674 (average)
