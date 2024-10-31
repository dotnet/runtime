# Contract CodeVersions

This contract encapsulates support for [code versioning](../features/code-versioning.md) in the runtime.

## APIs of contract

```csharp
internal struct NativeCodeVersionHandle
{
    // no public constructors
    internal readonly TargetPointer MethodDescAddress;
    internal readonly TargetPointer CodeVersionNodeAddress;
    internal NativeCodeVersionHandle(TargetPointer methodDescAddress, TargetPointer codeVersionNodeAddress)
    {
        if (methodDescAddress != TargetPointer.Null && codeVersionNodeAddress != TargetPointer.Null)
        {
            throw new ArgumentException("Only one of methodDescAddress and codeVersionNodeAddress can be non-null");
        }
        MethodDescAddress = methodDescAddress;
        CodeVersionNodeAddress = codeVersionNodeAddress;
    }

    internal static NativeCodeVersionHandle Invalid => new(TargetPointer.Null, TargetPointer.Null);
    public bool Valid => MethodDescAddress != TargetPointer.Null || CodeVersionNodeAddress != TargetPointer.Null;
}
```

```csharp
// Return a handle to the version of the native code that includes the given instruction pointer
public virtual NativeCodeVersionHandle GetNativeCodeVersionForIP(TargetCodePointer ip);
// Return a handle to the active version of the native code for a given method descriptor
public virtual NativeCodeVersionHandle GetActiveNativeCodeVersion(TargetPointer methodDesc);

// returns true if the given method descriptor supports multiple code versions
public virtual bool CodeVersionManagerSupportsMethod(TargetPointer methodDesc);

// Return the instruction pointer corresponding to the start of the given native code version
public virtual TargetCodePointer GetNativeCode(NativeCodeVersionHandle codeVersionHandle);
```

## Version 1

See [code versioning](../features/code-versioning.md) for a general overview and the definitions of *synthetic* and *explicit* nodes.

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| MethodDescVersioningState | Flags | `MethodDescVersioningStateFlags` flags, see below |
| MethodDescVersioningState | NativeCodeVersionNode | code version node of this method desc, if active |
| NativeCodeVersionNode | Next | pointer to the next native code version |
| NativeCodeVersionNode | MethodDesc | indicates a synthetic native code version node |
| NativeCodeVersionNode | NativeCode | indicates an explicit native code version node |
| ILCodeVersioningState | ActiveVersionKind | an `ILCodeVersionKind` value indicating which fields of the active version are value |
| ILCodeVersioningState | ActiveVersionNode | if the active version is explicit, the NativeCodeVersionNode for the active version |
| ILCodeVersioningState | ActiveVersionModule | if the active version is synthetic or unknown, the pointer to the Module that defines the method |
| ILCodeVersioningState | ActiveVersionMethodDef | if the active version is synthetic or unknown, the MethodDef token for the method |

The flag indicates that the default version of the code for a method desc is active:
```csharp
internal enum MethodDescVersioningStateFlags : byte
{
    IsDefaultVersionActiveChildFlag = 0x4
};
```

The value of the `ILCodeVersioningState::ActiveVersionKind` field is one of:
```csharp
private enum ILCodeVersionKind
{
    Unknown = 0,
    Explicit = 1, // means Node is set
    Synthetic = 2, // means Module and Token are set
}
```

Global variables used: *none*

Contracts used:
| Contract Name |
| --- |
| ExecutionManager |
| Loader |
| RuntimeTypeSystem |

### Finding the start of a specific native code version

```csharp
NativeCodeVersionHandle ICodeVersions.GetNativeCodeVersionForIP(TargetCodePointer ip)
{
    Contracts.IExecutionManager executionManager = _target.Contracts.ExecutionManager;
    EECodeInfoHandle? info = executionManager.GetEECodeInfoHandle(ip);
    if (!info.HasValue)
    {
        return NativeCodeVersionHandle.Invalid;
    }
    TargetPointer methodDescAddress = executionManager.GetMethodDesc(info.Value);
    if (methodDescAddress == TargetPointer.Null)
    {
        return NativeCodeVersionHandle.Invalid;
    }
    IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
    MethodDescHandle md = rts.GetMethodDescHandle(methodDescAddress);
    if (!rts.IsVersionable(md))
    {
        return new NativeCodeVersionHandle(methodDescAddress, codeVersionNodeAddress: TargetPointer.Null);
    }
    else
    {
        TargetCodePointer startAddress = executionManager.GetStartAddress(info.Value);
        return GetSpecificNativeCodeVersion(md, startAddress);
    }
}

NativeCodeVersionHandle GetSpecificNativeCodeVersion(MethodDescHandle md, TargetCodePointer startAddress)
{
    TargetPointer methodDescVersioningStateAddress = target.Contracts.RuntimeTypeSystem.GetMethodDescVersioningState(md);
    if (methodDescVersioningStateAddress == TargetPointer.Null)
    {
        return NativeCodeVersionHandle.Invalid;
    }
    Data.MethodDescVersioningState methodDescVersioningStateData = _target.ProcessedData.GetOrAdd<Data.MethodDescVersioningState>(methodDescVersioningStateAddress);
    return FindFirstCodeVersion(methodDescVersioningStateData, (codeVersion) =>
    {
        return codeVersion.MethodDesc == md.Address && codeVersion.NativeCode == startAddress;
    });
}

NativeCodeVersionHandle FindFirstCodeVersion(Data.MethodDescVersioningState versioningState, Func<Data.NativeCodeVersionNode, bool> predicate)
{
    TargetPointer currentAddress = versioningState.NativeCodeVersionNode;
    while (currentAddress != TargetPointer.Null)
    {
        Data.NativeCodeVersionNode current = _target.ProcessedData.GetOrAdd<Data.NativeCodeVersionNode>(currentAddress);
        if (predicate(current))
        {
            return new NativeCodeVersionHandle(methodDescAddress: TargetPointer.Null, currentAddress);
        }
        currentAddress = current.Next;
    }
    return NativeCodeVersionHandle.Invalid;
}
```

### Finding the active native code version of a method descriptor

```csharp
NativeCodeVersionHandle ICodeVersions.GetActiveNativeCodeVersion(TargetPointer methodDesc)
{
    IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
    MethodDescHandle md = rts.GetMethodDescHandle(methodDesc);
    TargetPointer mtAddr = rts.GetMethodTable(md);
    TypeHandle typeHandle = rts.GetTypeHandle(mtAddr);
    TargetPointer module = rts.GetModule(typeHandle);
    uint methodDefToken = rts.GetMethodToken(md);
    ILCodeVersionHandle methodDefActiveVersion = FindActiveILCodeVersion(module, methodDefToken);
    if (!methodDefActiveVersion.IsValid)
    {
        return NativeCodeVersionHandle.Invalid;
    }
    return FindActiveNativeCodeVersion(methodDefActiveVersion, methodDesc);
}

ILCodeVersionHandle ILCodeVersionHandleFromState(Data.ILCodeVersioningState ilState)
{
    switch ((ILCodeVersionKind)ilState.ActiveVersionKind)
    {
        case ILCodeVersionKind.Explicit:
            return new ILCodeVersionHandle(module: TargetPointer.Null, methodDef: 0, ilState.ActiveVersionNode);
        case ILCodeVersionKind.Synthetic:
        case ILCodeVersionKind.Unknown:
            return new ILCodeVersionHandle(ilState.ActiveVersionModule, ilState.ActiveVersionMethodDef, TargetPointer.Null);
        default:
            throw new InvalidOperationException($"Unknown ILCodeVersionKind {ilState.ActiveVersionKind}");
    }
}

ILCodeVersionHandle FindActiveILCodeVersion(TargetPointer module, uint methodDefinition)
{
    ModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandle(module);
    TargetPointer ilCodeVersionTable = _target.Contracts.Loader.GetLookupTables(moduleHandle).MethodDefToILCodeVersioningState;
    TargetPointer ilVersionStateAddress = _target.Contracts.Loader.GetModuleLookupMapElement(ilCodeVersionTable, methodDefinition, out var _);
    if (ilVersionStateAddress == TargetPointer.Null)
    {
        return new ILCodeVersionHandle(module, methodDefinition, TargetPointer.Null);
    }
    Data.ILCodeVersioningState ilState = _target.ProcessedData.GetOrAdd<Data.ILCodeVersioningState>(ilVersionStateAddress);
    return ILCodeVersionHandleFromState(ilState);
}

bool IsActiveNativeCodeVersion(NativeCodeVersionHandle nativeCodeVersion)
{
    if (nativeCodeVersion.MethodDescAddress != TargetPointer.Null)
    {
        MethodDescHandle md = _target.Contracts.RuntimeTypeSystem.GetMethodDescHandle(nativeCodeVersion.MethodDescAddress);
        TargetPointer versioningStateAddress = _target.Contracts.RuntimeTypeSystem.GetMethodDescVersioningState(md);
        if (versioningStateAddress == TargetPointer.Null)
        {
            return true;
        }
        Data.MethodDescVersioningState versioningState = _target.ProcessedData.GetOrAdd<Data.MethodDescVersioningState>(versioningStateAddress);
        MethodDescVersioningStateFlags flags = (MethodDescVersioningStateFlags)versioningState.Flags;
        return flags.HasFlag(MethodDescVersioningStateFlags.IsDefaultVersionActiveChildFlag);
    }
    else if (nativeCodeVersion.CodeVersionNodeAddress != TargetPointer.Null)
    {
        throw new NotImplementedException(); // TODO[cdac]: IsActiveNativeCodeVersion - explicit
    }
    else
    {
        throw new ArgumentException("Invalid NativeCodeVersionHandle");
    }
}

NativeCodeVersionHandle FindActiveNativeCodeVersion(ILCodeVersionHandle methodDefActiveVersion, TargetPointer methodDescAddress)
{
    if (methodDefActiveVersion.Module != TargetPointer.Null)
    {
        NativeCodeVersionHandle provisionalHandle = new NativeCodeVersionHandle(methodDescAddress: methodDescAddress, codeVersionNodeAddress: TargetPointer.Null);
        if (IsActiveNativeCodeVersion(provisionalHandle))
        {
            return provisionalHandle;
        }
        else
        {
            throw new NotImplementedException(); // TODO[cdac]: iterate through versioning state nodes
        }
    }
    else
    {
        throw new NotImplementedException(); // TODO: [cdac] find explicit il code version
    }
}
```

### Determining whether a method descriptor supports code versioning

```csharp
bool ICodeVersions.CodeVersionManagerSupportsMethod(TargetPointer methodDescAddress)
{
    IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
    MethodDescHandle md = rts.GetMethodDescHandle(methodDescAddress);
    if (rts.IsDynamicMethod(md))
        return false;
    if (rts.IsCollectibleMethod(md))
        return false;
    TargetPointer mtAddr = rts.GetMethodTable(md);
    TypeHandle mt = rts.GetTypeHandle(mtAddr);
    TargetPointer modAddr = rts.GetModule(mt);
    ILoader loader = _target.Contracts.Loader;
    ModuleHandle mod = loader.GetModuleHandle(modAddr);
    ModuleFlags modFlags = loader.GetFlags(mod);
    if (modFlags.HasFlag(ModuleFlags.EditAndContinue))
        return false;
    return true;
}
```
