# Contract ReJIT

This contract encapsulates support for [ReJIT](../features/code-versioning.md) in the runtime.

## APIs of contract

```csharp
public enum RejitState
{
    Requested,
    Active
}
```

```csharp
bool IsEnabled();

RejitState GetRejitState(ILCodeVersionHandle codeVersionHandle);

TargetNUInt GetRejitId(ILCodeVersionHandle codeVersionHandle);

IEnumerable<TargetNUInt> GetRejitIds(TargetPointer methodDesc)
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| ProfControlBlock | GlobalEventMask | an `ICorProfiler` `COR_PRF_MONITOR` value |
| ILCodeVersionNode | VersionId | `ILCodeVersion` ReJIT ID
| ILCodeVersionNode | RejitState | a `RejitFlags` value |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
|ProfilerControlBlock | TargetPointer | pointer to the `ProfControlBlock` |

Contracts used:
| Contract Name |
| --- |
| CodeVersions |

```csharp
// see src/coreclr/inc/corprof.idl
[Flags]
private enum COR_PRF_MONITOR
{
    COR_PRF_ENABLE_REJIT = 0x00040000,
}

// see src/coreclr/vm/codeversion.h
[Flags]
public enum RejitFlags : uint
{
    kStateRequested = 0x00000000,

    kStateGettingReJITParameters = 0x00000001,

    kStateActive = 0x00000002,

    kStateMask = 0x0000000F,

    kSuppressParams = 0x80000000
}

bool IsEnabled()
{
    TargetPointer address = target.ReadGlobalPointer("ProfilerControlBlock");
    ulong globalEventMask = target.Read<ulong>(address + /* ProfControlBlock::GlobalEventMask offset*/);
    bool profEnabledReJIT = (GlobalEventMask & (ulong)COR_PRF_MONITOR.COR_PRF_ENABLE_REJIT) != 0;
    bool clrConfigEnabledReJit = /* host process does not have environment variable DOTNET_ProfAPI_ReJitOnAttach set to 0 */;
    return profEnabledReJIT || clrConfigEnabledReJIT;
}

RejitState GetRejitState(ILCodeVersionHandle codeVersion)
{
    // ILCodeVersion::GetRejitState
    if (codeVersion is not explicit)
    {
        // for non explicit ILCodeVersions, ReJITState is always kStateActive
        return RejitState.Active;
    }
    else
    {
        // ILCodeVersionNode::GetRejitState
        ILCodeVersionNode codeVersionNode = AsNode(codeVersion);
        return ((RejitFlags)ilCodeVersionNode.RejitState & RejitFlags.kStateMask) switch
        {
            RejitFlags.kStateRequested => RejitState.Requested,
            RejitFlags.kStateActive => RejitState.Active,
            _ => throw new NotImplementedException($"Unknown ReJIT state: {ilCodeVersionNode.RejitState}"),
        };
    }
}

TargetNUInt GetRejitId(ILCodeVersionHandle codeVersion)
{
    // ILCodeVersion::GetVersionId
    if (codeVersion is not explicit)
    {
        // for non explicit ILCodeVersions, ReJITId is always 0
        return new TargetNUInt(0);
    }
    else
    {
        // ILCodeVersionNode::GetVersionId
        ILCodeVersionNode codeVersionNode = AsNode(codeVersion);
        return codeVersionNode.VersionId;
    }
}

IEnumerable<TargetNUInt> GetRejitIds(TargetPointer methodDesc)
{
    // ReJitManager::GetReJITIDs
    ICodeVersions cv = _target.Contracts.CodeVersions;
    IEnumerable<ILCodeVersionHandle> ilCodeVersions = cv.GetILCodeVersions(methodDesc);

    foreach (ILCodeVersionHandle ilCodeVersionHandle in ilCodeVersions)
    {
        if (GetRejitState(ilCodeVersionHandle) == RejitState.Active)
        {
            yield return GetRejitId(ilCodeVersionHandle);
        }
    }
}
```
