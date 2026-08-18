# EventPipe Tests

This directory contains tests for the EventPipe diagnostics subsystem. Most
tests use the shared `IpcTraceTest` infrastructure located in `common/`.

## How the tests work

Each test is a self-contained managed application that:

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
| `eventsvalidation/` | Tests that validate specific event types and payloads |
| `providervalidation/` | Tests that validate provider registration and event counts |
| `rundownvalidation/` | Tests for rundown event correctness |
| `config/`, `diagnosticport/`, `reverse*/` | Diagnostic port and configuration tests |
