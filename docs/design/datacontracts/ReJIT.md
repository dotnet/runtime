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
public enum OptimizationTierEnum
{
    Unknown = 0,
    MinOptJitted = 1,
    Optimized = 2,
    QuickJitted = 3,
    OptimizedTier1 = 4,
    ReadyToRun = 5,
    OptimizedTier1OSR = 6,
    QuickJittedInstrumented = 7,
    OptimizedTier1Instrumented = 8,
}
```

```csharp
bool IsEnabled();

RejitState GetRejitState(ILCodeVersionHandle codeVersionHandle);

TargetNUInt GetRejitId(ILCodeVersionHandle codeVersionHandle);

IEnumerable<TargetNUInt> GetRejitIds(TargetPointer methodDesc);
IEnumerable<(TargetPointer, TargetPointer, OptimizationTierEnum)> GetTieredVersions(TargetPointer methodDesc, int rejitId, int cNativeCodeAddrs);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| ProfControlBlock | GlobalEventMask | an `ICorProfiler` `COR_PRF_MONITOR` value |
| ILCodeVersionNode | VersionId | `ILCodeVersion` ReJIT ID
| ILCodeVersionNode | RejitState | a `RejitFlags` value |
| MethodDesc | CodeData | Pointer to CodeData (additional info about native code) |
| MethodDescCodeData | OptimizationTier | Optimization tier of default native code version |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
|ProfilerControlBlock | TargetPointer | pointer to the `ProfControlBlock` |
| `EEConfig` | TargetPointer | Pointer to the global EEConfig |

Contracts used:
| Contract Name |
| --- |
| CodeVersions |
| RuntimeTypeSystem |
| Loader |

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

private enum NativeOptimizationTier : uint
{
    OptimizationTier0 = 0,
    OptimizationTier1 = 1,
    OptimizationTier1OSR = 2,
    OptimizationTierOptimized = 3,
    OptimizationTier0Instrumented = 4,
    OptimizationTier1Instrumented = 5,
    OptimizationTierUnknown = 0xFFFFFFFF
};
```
```csharp

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

IEnumerable<(TargetPointer, TargetPointer, OptimizationTierEnum)> GetTieredVersions(TargetPointer methodDesc, int rejitId, int cNativeCodeAddrs)
{
    ICodeVersions codeVersionsContract = target.Contracts.CodeVersions;
    IReJIT rejitContract = this;

    ILCodeVersionHandle ilCodeVersion = codeVersionsContract.GetILCodeVersions(methodDesc)
    MethodDescHandle mdh = rts.GetMethodDescHandle(methodDesc);
        .FirstOrDefault(ilcode => rejitContract.GetRejitId(ilcode).Value == (ulong)rejitId,
            ILCodeVersionHandle.Invalid);

    if (!ilCodeVersion.IsValid)
        throw new ArgumentException();
    // Iterate through versioning state nodes and return the active one, matching any IL code version
    IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
    ILoader loader = target.Contracts.Loader;
    ModuleHandle moduleHandle = // get the module handle from the method desc using rts

    bool isReadyToRun = loader.GetReadyToRunImageInfo(moduleHandle, out TargetPointer r2rImageBase, out TargetPointer r2rImageEnd);
    bool isEligibleForTieredCompilation = rts.IsEligibleForTieredCompilation(mdh);
    int count = 0;
    foreach (NativeCodeVersionHandle nativeCodeVersionHandle in codeVersionsContract.GetNativeCodeVersions(methodDesc, ilCodeVersion))
    {
        TargetPointer nativeCode = codeVersionsContract.GetNativeCode(nativeCodeVersionHandle).AsTargetPointer;
        TargetPointer nativeCodeAddr = nativeCode;
        TargetPointer nativeCodeVersionNodePtr = nativeCodeVersionHandle.IsExplicit ? AsNode(nativeCodeVersionHandle).Address : TargetPointer.Null;
        OptimizationTierEnum optimizationTier;
        if (r2rImageBase <= nativeCode && nativeCode < r2rImageEnd)
        {
            optimizationTier = OptimizationTierEnum.ReadyToRun;
        }

        else if (isEligibleForTieredCompilation)
        {
            NativeOptimizationTier optTier;
            if (!nativeCodeVersionHandle.IsExplicit)
                optTier = GetInitialOptimizationTier(mdh);
            else
            {
                optTier = (NativeOptimizationTier)target.ReadPointer(/* native code version address + NativeCodeVersionNode::OptimizationTier offset */);
            }
            optimizationTier = GetOptimizationTier(optTier);
        }
        else if (rts.IsJitOptimizationDisabled(mdh))
        {
            optimizationTier = OptimizationTierEnum.MinOptJitted;
        }
        else
        {
            optimizationTier = OptimizationTierEnum.Optimized;
        }
        count++;
        yield return (nativeCodeAddr, nativeCodeVersionNodePtr, optimizationTier);
    }
}

private NativeOptimizationTier GetInitialOptimizationTier(TargetPointer mdPointer)
{
    // validation of the method desc
    MethodDescHandle _ = target.Contracts.RuntimeTypeSystem.GetMethodDescHandle(mdPointer);
    TargetPointer codeData = target.ReadPointer(mdPointer + /* MethodDesc::CodeData offset */);
    return (NativeOptimizationTier)target.Read<uint>(codeData + /* MethodDescCodeData::OptimizationTier offset */);
}

private static OptimizationTierEnum GetOptimizationTier(NativeOptimizationTier nativeOptimizationTier)
{
    return nativeOptimizationTier switch
    {
        NativeOptimizationTier.OptimizationTier0 => OptimizationTierEnum.QuickJitted,
        NativeOptimizationTier.OptimizationTier1 => OptimizationTierEnum.OptimizedTier1,
        NativeOptimizationTier.OptimizationTier1OSR => OptimizationTierEnum.OptimizedTier1OSR,
        NativeOptimizationTier.OptimizationTierOptimized => OptimizationTierEnum.Optimized,
        NativeOptimizationTier.OptimizationTier0Instrumented => OptimizationTierEnum.QuickJittedInstrumented,
        NativeOptimizationTier.OptimizationTier1Instrumented => OptimizationTierEnum.OptimizedTier1Instrumented,
        _ => OptimizationTierEnum.Unknown,
    };
}
```
