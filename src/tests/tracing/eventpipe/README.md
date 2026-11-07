# EventPipe Tests

This directory contains tests for the EventPipe diagnostics subsystem. Most
tests use the shared `IpcTraceTest` infrastructure located in `common/`.

## How the tests work

The managed tracing tests are self-contained applications that:

1. Creates a `DiagnosticsClient` connected to its own process.
2. Starts an EventPipe session with one or more providers.
3. Runs an event-generating action (e.g., GC collections, CPU work, exceptions).
4. Reads the event stream back and validates event counts and/or payloads.
5. Returns exit code **100** on success or a non-zero value on failure.

The orchestration is handled by `IpcTraceTest.RunAndValidateEventCounts()` in
`common/IpcTraceTest.cs`.

## Writing a new test

Use an existing test as a template. `eventsvalidation/GCEvents.cs` and
`GCEvents.csproj` are good starting points.

### Project file

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <RequiresProcessIsolation>true</RequiresProcessIsolation>
    <TargetFrameworkIdentifier>.NETCoreApp</TargetFrameworkIdentifier>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <CLRTestPriority>1</CLRTestPriority>
    <UnloadabilityIncompatible>true</UnloadabilityIncompatible>
    <GCStressIncompatible>true</GCStressIncompatible>
    <JitOptimizationSensitive>true</JitOptimizationSensitive>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="MyTest.cs" />
    <ProjectReference Include="../common/eventpipe_common.csproj" />
    <ProjectReference Include="../common/Microsoft.Diagnostics.NETCore.Client/Microsoft.Diagnostics.NETCore.Client.csproj" />
    <ProjectReference Include="$(TestLibraryProjectPath)" />
  </ItemGroup>
</Project>
```

### Test structure

```csharp
var providers = new List<EventPipeProvider>()
{
    new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose)
};

return IpcTraceTest.RunAndValidateEventCounts(
    expectedEventCounts,
    eventGeneratingAction,
    providers,
    1024,
    optionalTraceValidator);
```

### Accessing event payloads

Register a handler via `source.Dynamic.All` and filter by `ProviderName`:

```csharp
source.Dynamic.All += (eventData) =>
{
    if (eventData.ProviderName != "Microsoft-DotNETCore-SampleProfiler")
        return;

    // Read the raw payload bytes (works for all events):
    Span<byte> data = eventData.EventData().AsSpan();
    uint value = BitConverter.ToUInt32(data.Slice(0, 4));

    // Or use named fields (only for events with well-known TraceEvent schemas):
    // object obj = eventData.PayloadByName("fieldName");
};
```

### Validation callback

The optional validator has the signature `Func<EventPipeEventSource, Func<int>>`.
Register event handlers in the outer function, then return an inner `Func<int>`
that checks the collected data and returns `100` (pass) or `-1` (fail).

### Non-lossy (Block) buffering

By default a session drops events when its buffer fills. Passing a `bufferingMode`
of `EventPipeBufferingMode.Block` instead parks the producer until the drain thread
frees capacity, so no events are lost. Pair it with a small `circularBufferMB` to
exercise the parking path and `failOnEventsLost` to assert that nothing was dropped:

```csharp
return IpcTraceTest.RunAndValidateEventCounts(
    expectedEventCounts,
    eventGeneratingAction,
    providers,
    circularBufferMB: 1,
    bufferingMode: EventPipeBufferingMode.Block,
    failOnEventsLost: true);
```

## Common providers

| Provider | Purpose |
|----------|---------|
| `Microsoft-DotNETCore-SampleProfiler` | CPU sampling (ThreadSample events) |
| `Microsoft-Windows-DotNETRuntime` | CLR runtime events (GC, exceptions, JIT, etc.) |
| `Microsoft-Windows-DotNETRuntimeRundown` | Rundown events (method symbols, modules) |

## Subdirectory layout

| Directory | Contents |
|-----------|----------|
| `common/` | Shared infrastructure (`IpcTraceTest`, `DiagnosticsClient`, helpers) |
| `native/` | Native EventPipe and container unit tests, managed launchers, and shared native-test runner |
| `eventsvalidation/` | Tests that validate specific event types and payloads |
| `providervalidation/` | Tests that validate provider registration and event counts |
| `rundownvalidation/` | Tests for rundown event correctness |
| `config/`, `diagnosticport/`, `reverse*/` | Diagnostic port and configuration tests |

## Opt-in native container performance suite

The SIMD hash-table correctness tests, performance measurements, and private
comparison hash table are implemented together in
`native/containers/dn-simdhash-tests.c` and compiled into
`NativeContainerTestsDriver` during ordinary native test builds. There is no
separate benchmark executable, build target, or build script. The generic
container `perf` group registers `test_simdhash_perf`; other container performance
tests can be added to `native/containers/dn-container-perf-tests.c`.

The `perf` group is excluded from unfiltered native runs and `--list`; use
`--list-all` to include opt-in groups in discovery. The managed
`NativeContainerTests.RunPerformanceSuite` test is skipped unless
`DOTNET_TEST_NATIVE_CONTAINER_PERF=1`, so normal CI builds the suite without
executing it.

Standalone managed launchers do not support method-name filtering: enabling this
flag runs the performance test in addition to the correctness tests. Use the
native `perf` invocation below to run only the performance suite.

For example, from the built container-test directory in Windows cmd.exe, with
`CORE_ROOT` set to the matching runtime layout:

```bat
set DOTNET_TEST_NATIVE_CONTAINER_PERF=1
"%CORE_ROOT%\corerun.exe" NativeContainerTests.dll
set DOTNET_TEST_NATIVE_CONTAINER_PERF=
```

For NativeAOT, run the generated `NativeContainerTests` executable with the same
opt-in environment variable. To invoke only the native suite directly:

```bat
NativeContainerTestsDriver.exe perf
```

Use a Release native test build for performance measurements. All 18 original
workloads and their warmup and measurement durations are preserved, so a complete
run takes approximately seven minutes, plus setup overhead. The opt-in managed
suite has a fifteen-minute native-process timeout; ordinary native test calls
retain their sixty-second timeout. Timings are printed for local comparison,
not used as pass/fail thresholds. Measurements use minipal's high-resolution
monotonic timer and report elapsed time, not process CPU time, on all platforms.

## Opt-in native EventPipe performance suite

The buffer, buffer-manager, and event-writing performance tests are compiled into
`NativeEventPipeTestsDriver` during ordinary native test builds in every
configuration. There is no separate performance build option. As with containers,
the `perf` group is excluded from unfiltered native runs and `--list`; use
`--list-all` to discover it or select `perf` explicitly to execute it.

The suite is registered in `native/eventpipe/ep-perf-tests.c`, matching the
container performance-suite registry. Individual benchmark implementations stay
in their subsystem test files alongside the helpers they use.

The managed `NativeEventPipeTests.RunPerformanceSuite` test is skipped unless
`DOTNET_TEST_NATIVE_EVENTPIPE_PERF=1`. As with containers, the standalone managed
launcher runs the performance test in addition to its correctness tests and does
not support method-name filtering. From the built EventPipe-test directory in
Windows cmd.exe, with `CORE_ROOT` set to the matching runtime layout:

```bat
set DOTNET_TEST_NATIVE_EVENTPIPE_PERF=1
"%CORE_ROOT%\corerun.exe" NativeEventPipeTests.dll
set DOTNET_TEST_NATIVE_EVENTPIPE_PERF=
```

For NativeAOT, run the generated `NativeEventPipeTests` executable with the same
opt-in environment variable. To invoke only the native suite directly:

```bat
NativeEventPipeTestsDriver.exe perf
```

Use a Release native test build for meaningful performance measurements. The
opt-in managed suite has a fifteen-minute native-process timeout; ordinary native
test calls retain their sixty-second timeout. Normal CI builds the performance
code without running it, and performance timings have no pass/fail thresholds.
