# Contract ReJIT

This contract encapsulates support for [ReJIT](../features/code-versioning.md) in the runtime.

## APIs of contract

```csharp
bool IsEnabled();
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| ProfControlBlock | GlobalEventMask | an `ICorProfiler` `COR_PRF_MONITOR` value |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
|ProfilerControlBlock | TargetPointer | pointer to the `ProfControlBlock` |

Contracts used:
| Contract Name |
| --- |

```csharp
// see src/coreclr/inc/corprof.idl
[Flags]
private enum COR_PRF_MONITOR
{
    COR_PRF_ENABLE_REJIT = 0x00040000,
}

bool IsEnabled()
{
    TargetPointer address = target.ReadGlobalPointer("ProfilerControlBlock");
    ulong globalEventMask = target.Read<ulong>(address + /* ProfControlBlock::GlobalEventMask offset*/);
    bool profEnabledReJIT = (GlobalEventMask & (ulong)COR_PRF_MONITOR.COR_PRF_ENABLE_REJIT) != 0;
    bool clrConfigEnabledReJit = /* host process does not have environment variable DOTNET_ProfAPI_ReJitOnAttach set to 0 */;
    return profEnabledReJIT || clrConfigEnabledReJIT;
}
```
