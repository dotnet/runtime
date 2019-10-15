Garbage Collector Requirements
==============================

The garbage collector is an integral part of the .NET runtime.  Changes to the .NET runtime can have significant effects on program correctness and performance.  As such, GC has strong implementation and testing requirements.

We strongly recommend that if you want to make a change to GC, that you share your proposal prior to implementation.  This will ensure that if there are any questions or concerns, that they are addressed before significant work is done.

# Requirements by Sub-Component #

## Core GC ##
The native implementation of the GC - including the GCHeap and gc_heap classes.

**Required Testing:** (Instructions for running tests are below)

- **gc_heap class:** Changes to gc_heap requires a 48 hour stress run on a debug build.  This helps to ensure correctness of the change given that the GC runs in many different configurations and interacts with many other sub-systems within the runtime.  For some simple changes a stress run may not be required.  If you believe your change does not manipulate the heap itself, mention this in your pull request and we can tell you if stress is required or not.
- **GCHeap class:**  GCHeap changes generally do not require stress runs, but do require functional testing of the affected code.
- **Performance changes:** Performance specific changes require a performance test run.

## Managed APIs ##
Managed APIs in System.GC and System.Runtime.GCSettings.  These APIs allow an application developer to view and modify GC options.

Required Testing: Validation of the behavior of the affected APIs.

# Instructions for Testing #

## Stress Testing ##
Stress testing must run for at least **48 hours** against a debug build.

Stress testing for checked and release builds can be run locally. Please following the instructions described in tests\src\GC\Stress\stress_run_readme.txt. You can also request it on pull requests with The .NET CI infrastructure with the trigger phrase:

```
@dotnet_bot test <platform> <flavor> gc_reliability_framework
```

This will run the stress framework for the default amount of time (15 hours) on the given platform and build flavor.

## Functional Testing ##
A functional test run executes the same code as a stress run, but only runs for 30 minutes.

It is recommended that you run at least some of the below PR-triggered CI jobs:

```
@dotnet_bot test Windows_NT Checked longgc
@dotnet_bot test OSX10.12 Checked longgc
@dotnet_bot test Ubuntu Checked longgc
@dotnet_bot test Windows_NT Checked standalone_gc
@dotnet_bot test OSX10.12 Checked standalone_gc
@dotnet_bot test Ubuntu Checked standalone_gc
```

The "Long GC" tests are a series of GC tests whose running time is too long or memory usage is too high to run with
the rest of the Priority 0 unit tests. The "Standalone GC" build mode builds and runs the GC in a semi-standalone manner
(see https://github.com/dotnet/coreclr/projects/3).

You may also wish to run the GC Simulator tests. They may take up to 24 hours to complete and are known to sometimes fail on Ubuntu
due to poor interactions with the Linux OOM killer. However, they have proven to be quite useful in finding bugs in the past:

```
@dotnet_bot test Windows_NT Release gcsimulator
@dotnet_bot test Ubuntu Release gcsimulator
@dotnet_bot test OSX10.12 Release gcsimulator
```

## Performance Testing ##
Coming soon.
