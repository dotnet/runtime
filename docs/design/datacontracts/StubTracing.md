# Contract StubTracing

This contract traces runtime stubs to the code that they eventually invoke.
It models one tracing step. The legacy `IXCLRDataProcess` implementation owns
the opaque continuation-buffer encoding.

## APIs of contract

```csharp
public enum StubTraceKind
{
    Unknown,
    Failed,
    Managed,
    Unmanaged,
    UnjittedMethod,
    FramePush,
}

public enum StubContinuationKind : ulong
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

public interface IStubTracing : IContract
{
    StubTraceStep TraceStubStep(
        TargetCodePointer address,
        StubContinuation continuation,
        TargetPointer thread);
}
```

## Version 1

Version 1 classifies code through the ExecutionManager contract. Managed code
terminates tracing. Jump stubs and call-counting stubs are followed and
reclassified. Precodes resolve to native code when available or produce a
method-jitted continuation. The prestub produces a frame-push continuation.
Virtual dispatch stubs are not implemented. JITted IL stubs, P/Invoke methods,
and async thunks are
claimed by native stub managers that cannot trace them in the DAC build, so they
are not reported as final managed code. P/Invoke import and unmanaged-entry
precodes are not traced. The prestub is not traceable on Apple ARM64.
Expected tracing failures return a step with `StubTraceKind.Failed`. Invalid
continuation data throws an argument exception.

<!-- BEGIN GENERATED: usage contract=StubTracing version=c1 -->
### Data descriptors used

| Data Descriptor | Field | Type | Meaning |
| --- | --- | --- | --- |
| `CallCountingStubData` | `TargetForMethod` | `CodePointer` | Target invoked while the call-counting stub remains active |
| `PrecodeMachineDescriptor` | `StubCodePageSize` | `uint32` | Size of a precode code page (in bytes) |

### Global variables used

| Global | Type | Meaning |
| --- | --- | --- |
| `DACNotifyCompilationFinished` | `pointer` | Breakpoint address used while waiting for an unjitted method |
| `ThePreStub` | `pointer` | Address of the global containing the prestub entrypoint |
| `ThePreStubPatchLabel` | `pointer` | Breakpoint address used by prestub frame tracing |

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
