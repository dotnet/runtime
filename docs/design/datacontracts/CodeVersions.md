# Contract CodeVersions

This contract encapsulates support for [code versioning](../features/code-versioning.md) in the runtime.

## APIs of contract

```csharp
internal readonly struct ILCodeVersionHandle
{
    public static ILCodeVersionHandle Invalid;

    public bool IsValid;
}
```

```csharp
internal struct NativeCodeVersionHandle
{
    internal static NativeCodeVersionHandle Invalid;

    public bool Valid;
}
```

```csharp
// Return a handle to the active version of the IL code for a given method descriptor
public virtual ILCodeVersionHandle GetActiveILCodeVersion(TargetPointer methodDesc);
// Return a handle to the IL code version representing the given native code version
public virtual ILCodeVersionHandle GetILCodeVersion(NativeCodeVersionHandle codeVersionHandle);
// Return all of the IL code versions for a given method descriptor
public virtual IEnumerable<ILCodeVersionHandle> GetILCodeVersions(TargetPointer methodDesc);

// Return a handle to the version of the native code that includes the given instruction pointer
public virtual NativeCodeVersionHandle GetNativeCodeVersionForIP(TargetCodePointer ip);
// Return a handle to the active version of the native code for a given method descriptor and IL code version. The IL code version and method descriptor must represent the same method
public virtual NativeCodeVersionHandle GetActiveNativeCodeVersionForILCodeVersion(TargetPointer methodDesc, ILCodeVersionHandle ilCodeVersionHandle);

// returns true if the given method descriptor supports multiple code versions
public virtual bool CodeVersionManagerSupportsMethod(TargetPointer methodDesc);

// Return the instruction pointer corresponding to the start of the given native code version
public virtual TargetCodePointer GetNativeCode(NativeCodeVersionHandle codeVersionHandle);
```
### Extension Methods
```csharp
// Return a handle to the active version of the native code for a given method descriptor
public static NativeCodeVersionHandle GetActiveNativeCodeVersion(this ICodeVersions, TargetPointer methodDesc);
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
| NativeCodeVersionNode | Flags | `NativeCodeVersionNodeFlags` flags, see below |
| NativeCodeVersionNode | VersionId | Version ID corresponding to the parent IL code version |
| ILCodeVersioningState | FirstVersionNode | pointer to the first `ILCodeVersionNode` |
| ILCodeVersioningState | ActiveVersionKind | an `ILCodeVersionKind` value indicating which fields of the active version are value |
| ILCodeVersioningState | ActiveVersionNode | if the active version is explicit, the NativeCodeVersionNode for the active version |
| ILCodeVersioningState | ActiveVersionModule | if the active version is synthetic or unknown, the pointer to the Module that defines the method |
| ILCodeVersioningState | ActiveVersionMethodDef | if the active version is synthetic or unknown, the MethodDef token for the method |
| ILCodeVersionNode | VersionId | Version ID of the node |
| ILCodeVersionNode | Next | Pointer to the next `ILCodeVersionNode`|

The flag indicates that the default version of the code for a method desc is active:
```csharp
internal enum MethodDescVersioningStateFlags : byte
{
    IsDefaultVersionActiveChildFlag = 0x4
};
```

The flag indicates the native code version is active:
```csharp
internal enum NativeCodeVersionNodeFlags : uint
{
    IsActiveChild = 1
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

### Finding active ILCodeVersion for a method
```csharp
public virtual ILCodeVersionHandle GetActiveILCodeVersion(TargetPointer methodDesc);
```
1. Check if the method has an `ILCodeVersioningState`.
2. If the method does not have an `ILCodeVersioningState`, the synthetic ILCodeVersion must be active. Return the synthetic ILCodeVersion for the method.
3. Otherwise, read the active ILCodeVersion off of the `ILCodeVersioningState`.

### Finding ILCodeVersion from a NativeCodeVersion
```csharp
public virtual ILCodeVersionHandle GetILCodeVersion(NativeCodeVersionHandle nativeCodeVersionHandle);
```
1. If `nativeCodeVersionHandle` is invalid, return an invalid `ILCodeVersionHandle`.
2. If `nativeCodeVersionHandle` is synthetic, the corresponding ILCodeVersion must also be synthetic; return the synthetic ILCodeVersion for the method.
3. Search the linked list of ILCodeVersions for one with the matching ILVersionId. Return the ILCodeVersion if found. Otherwise return invalid.

### Finding all of the ILCodeVersions for a method
```csharp
IEnumerable<ILCodeVersionHandle> ICodeVersions.GetILCodeVersions(TargetPointer methodDesc)
{
    // CodeVersionManager::GetILCodeVersions
    GetModuleAndMethodDesc(methodDesc, out TargetPointer module, out uint methodDefToken);

    ModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandle(module);
    TargetPointer ilCodeVersionTable = _target.Contracts.Loader.GetLookupTables(moduleHandle).MethodDefToILCodeVersioningState;
    TargetPointer ilVersionStateAddress = _target.Contracts.Loader.GetModuleLookupMapElement(ilCodeVersionTable, methodDefToken, out var _);

    // always add the synthetic version
    yield return new ILCodeVersionHandle(module, methodDefToken, TargetPointer.Null);

    // if explicit versions exist, iterate linked list and return them
    if (ilVersionStateAddress != TargetPointer.Null)
    {
        Data.ILCodeVersioningState ilState = _target.ProcessedData.GetOrAdd<Data.ILCodeVersioningState>(ilVersionStateAddress);
        TargetPointer nodePointer = ilState.FirstVersionNode;
        while (nodePointer != TargetPointer.Null)
        {
            Data.ILCodeVersionNode current = _target.ProcessedData.GetOrAdd<Data.ILCodeVersionNode>(nodePointer);
            yield return new ILCodeVersionHandle(TargetPointer.Null, 0, nodePointer);
            nodePointer = current.Next;
        }
    }
}
```

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
        return NativeCodeVersion.OfSynthetic(methodDescAddress);
    }
    else
    {
        TargetCodePointer startAddress = executionManager.GetStartAddress(info.Value);
        return GetSpecificNativeCodeVersion(md, startAddress);
    }
}

NativeCodeVersionHandle GetSpecificNativeCodeVersion(MethodDescHandle md, TargetCodePointer startAddress)
{
    // "Initial" stage of NativeCodeVersionIterator::Next() with a null m_ilCodeFilter
    TargetCodePointer firstNativeCode = rts.GetNativeCode(md);
    if (firstNativeCode == startAddress)
    {
        NativeCodeVersionHandle first = NativeCodeVersionHandle.OfSynthetic(md.Address);
        return first;
    }

    return FindFirstCodeVersion(rts, md, (codeVersion) =>
    {
        return codeVersion.MethodDesc == md.Address && codeVersion.NativeCode == startAddress;
    });
}

NativeCodeVersionHandle FindFirstCodeVersion(IRuntimeTypeSystem rts, MethodDescHandle md, Func<Data.NativeCodeVersionNode, bool> predicate)
{
    // ImplicitCodeVersion stage of NativeCodeVersionIterator::Next()
    TargetPointer versioningStateAddr = rts.GetMethodDescVersioningState(md);
    if (versioningStateAddr == TargetPointer.Null)
        return NativeCodeVersionHandle.Invalid;

    Data.MethodDescVersioningState versioningState = _target.ProcessedData.GetOrAdd<Data.MethodDescVersioningState>(versioningStateAddr);

    // LinkedList stage of NativeCodeVersion::Next, heavily inlined
    TargetPointer currentAddress = versioningState.NativeCodeVersionNode;
    while (currentAddress != TargetPointer.Null)
    {
        Data.NativeCodeVersionNode current = _target.ProcessedData.GetOrAdd<Data.NativeCodeVersionNode>(currentAddress);
        if (predicate(current))
        {
            return NativeCodeVersionHandle.OfExplicit(currentAddress);
        }
        currentAddress = current.Next;
    }
    return NativeCodeVersionHandle.Invalid;
}
```

### Finding the active native code version of an ILCodeVersion for a method descriptor
```csharp
public virtual NativeCodeVersionHandle GetActiveNativeCodeVersionForILCodeVersion(TargetPointer methodDesc, ILCodeVersionHandle ilCodeVersionHandle);
```

1. If `ilCodeVersionHandle` is invalid, return invalid.
2. If `ilCodeVersionHandle` is synthetic, the active native code version could be synthetic. Check if the method's synthetic NativeCodeVersion is active. If it is, return that NativeCodeVersion.
3. Search the linked list of NativeCodeVersions for one with the active flag and the relevent ILVersionId. If found return that node. Otherwise return invalid.

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
