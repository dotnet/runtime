# Contract Debugger

This contract is for reading debugger state from the target process, including initialization status, configuration flags, metadata update state, and JIT attach state.

## APIs of contract

```csharp
bool IsLeftSideInitialized();
uint GetDefinesBitField();
uint GetMDStructuresVersion();
int GetAttachStateFlags();
bool MetadataUpdatesApplied();
```

## Version 1

### Globals

| Global Name | Type | Description |
| --- | --- | --- |
| `Debugger` | TargetPointer | Pointer to the Debugger instance |
| `CLRJitAttachState` | TargetPointer | Pointer to the CLR JIT attach state flags |
| `MetadataUpdatesApplied` | TargetPointer | Pointer to the g_metadataUpdatesApplied flag |

### Data Descriptors

| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `Debugger` | `LeftSideInitialized` | Whether the left-side debugger infrastructure is initialized |
| `Debugger` | `Defines` | Bitfield of compile-time debugger feature defines |
| `Debugger` | `MDStructuresVersion` | Version of metadata data structures |

### Algorithm

```csharp
bool IsLeftSideInitialized()
{
    TargetPointer debuggerPtr = target.ReadGlobalPointer("Debugger");
    if (debuggerPtr == TargetPointer.Null)
        return false;
    Data.Debugger debugger = target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerPtr);
    return debugger.LeftSideInitialized != 0;
}

uint GetDefinesBitField()
{
    TargetPointer debuggerPtr = target.ReadGlobalPointer("Debugger");
    if (debuggerPtr == TargetPointer.Null)
        throw COMException(CORDBG_E_NOTREADY);
    Data.Debugger debugger = target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerPtr);
    return debugger.Defines;
}

uint GetMDStructuresVersion()
{
    TargetPointer debuggerPtr = target.ReadGlobalPointer("Debugger");
    if (debuggerPtr == TargetPointer.Null)
        throw COMException(CORDBG_E_NOTREADY);
    Data.Debugger debugger = target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerPtr);
    return debugger.MDStructuresVersion;
}

int GetAttachStateFlags()
{
    if (target.TryReadGlobalPointer("CLRJitAttachState", out TargetPointer addr))
        return (int)target.Read<uint>(addr);
    return 0;
}

bool MetadataUpdatesApplied()
{
    if (target.TryReadGlobalPointer("MetadataUpdatesApplied", out TargetPointer addr))
        return target.Read<byte>(addr) != 0;
    return false;
}
```
