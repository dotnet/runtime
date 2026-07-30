# Contract StubTracing

This contract traces runtime stubs to the code that they eventually invoke.
It models one tracing step.

## APIs of contract

```csharp
public enum StubTraceKind
{
    Unknown,
    Failed,
    Managed,
    UnjittedMethod,
    FramePush,
}

public enum StubContinuationKind
{
    None,
    MethodJitted,
    FramePush,
}

public readonly record struct StubContinuation(
    StubContinuationKind Kind,
    TargetPointer MethodDesc,
    TargetCodePointer Address);

public readonly record struct StubTraceStep(
    StubTraceKind Kind,
    TargetCodePointer Address,
    StubContinuation Continuation);

StubTraceStep TraceStubStep(
        TargetCodePointer address,
        StubContinuation continuation,
        TargetPointer thread);
```

<!-- BEGIN GENERATED: usage contract=StubTracing version=c1 -->
### Data descriptors used

| Data Descriptor | Field | Type | Meaning |
| --- | --- | --- | --- |
| `CallCountingStubData` | `TargetForMethod` | `CodePointer` | Target invoked while the call-counting stub remains active |
| `PrecodeMachineDescriptor` | `StubCodePageSize` | `uint32` | Size of a precode code page (in bytes) |

### Global variables used

| Global | Type | Meaning |
| --- | --- | --- |
| `DACNotifyCompilationFinished` | `pointer` | Address of the global containing the breakpoint used while waiting for an unjitted method |
| `ThePreStub` | `pointer` | Address of the global containing the prestub entrypoint |
| `ThePreStubPatchLabel` | `pointer` | Address of the global containing the breakpoint used by prestub frame tracing |

### Contracts used

| Contract Name |
| --- |
| `ExecutionManager` |
| `PlatformMetadata` |
| `PrecodeStubs` |
| `RuntimeInfo` |
| `RuntimeTypeSystem` |
| `StackWalk` |
| `Thread` |
<!-- END GENERATED: usage contract=StubTracing version=c1 -->

### Tracing a single stub step

```csharp
StubTraceStep TraceStubStep(TargetCodePointer address, StubContinuation continuation, TargetPointer thread)
{
    // Follows a stub through one step. Either we are able to follow it to a piece of jitted code,
    // in which the step address holds the final native code address, and the continuation is empty,
    // or we are not. In that case, the step address may be an intermediate address, such as the address of
    // DACNotifyCompilationFinished. The step continuation then contains information such as a MethodDesc
    // of a to-be-jitted method that can be used to finally resolve the stub at a later time.

    // Limitations:
    // Virtual dispatch stubs are not implemented.
    // StubLinkStubManager logic is not implemented.
    // JITted IL stubs, P/Invoke methods, and async thunks are
    // claimed by native stub managers whose logic is NYI in the managed implementation, so they
    // are not reported as final managed code.
    // P/Invoke import and unmanaged-entry precodes are not traced.
    // The prestub is not traceable on Apple ARM64.
}
```
