# Profiler Tests

## Architecture
A profiler attaches to a managed process via environment variables that need to be set before the process is launched. Our profiler tests do the test verification in the native profiler code, so the output of the profiler needs to be verified. To achieve both of these things the profiler tests are a wrapper that re-launch themselves with the correct environment variables set and also verify that the profiler succeeded in its verification.

## How to run one to investigate
Because of this two layer architecture just running the managed test executable will not run the test. You have to set the appropriate environment variables and pass the RunTest argument. The easiest way to create a script like the following

```
#~/bin/bash

export CORECLR_ENABLE_PROFILING=1
export CORECLR_PROFILER={2726B5B4-3F88-462D-AEC0-4EFDC8D7B921}
export CORECLR_PROFILER_PATH=<Path to test binaries>/profiler/eventpipe/eventpipe/libProfiler.so

<Path to test binaries>/Core_Root/corerun <Path to test binaries>/profiler/eventpipe/eventpipe/eventpipe.dll RunTest
```
