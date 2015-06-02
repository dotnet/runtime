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

Instructions for running stress are located in the repo at tests\src\GC\Stress\stress_run_readme.txt.

## Functional Testing ##
A functional test run executes the same code as a stress run, but only runs for 30 minutes.

Instructions for running stress are located in the repo at tests\src\GC\Stress\stress_run_readme.txt.

## Performance Testing ##
Coming soon.
