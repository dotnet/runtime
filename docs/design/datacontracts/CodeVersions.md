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
    public virtual NativeCodeVersionHandle GetSpecificNativeCodeVersion(TargetCodePointer ip) => throw new NotImplementedException();
    // Return a handle to the active version of the native code for a given method descriptor
    public virtual NativeCodeVersionHandle GetActiveNativeCodeVersion(TargetPointer methodDesc) => throw new NotImplementedException();

    // returns true if the given method descriptor supports multiple code versions
    public virtual bool CodeVersionManagerSupportsMethod(TargetPointer methodDesc) => throw new NotImplementedException();

    // Return the instruction pointer corresponding to the start of the given native code version
    public virtual TargetCodePointer GetNativeCode(NativeCodeVersionHandle codeVersionHandle) => throw new NotImplementedException();
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| MethodDescVersioningState | ? | ? |
| NativeCodeVersionNode | ? | ? |
| ILCodeVersioningState | ? | ? |


Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |

Contracts used:
| Contract Name |
| --- |
| ExecutionManager |
| Loader |
| RuntimeTypeSystem |

### Finding the start of a specific native code version

```csharp
    NativeCodeVersionHandle GetSpecificNativeCodeVersion(TargetCodePointer ip)
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

    private NativeCodeVersionHandle GetSpecificNativeCodeVersion(MethodDescHandle md, TargetCodePointer startAddress)
    {
        TargetPointer methodDescVersioningStateAddress = target.Contracts.RuntimeTypeSystem.GetMethodDescVersioningState(md);
        if (methodDescVersioningStateAddress == TargetPointer.Null)
        {
            return NativeCodeVersionHandle.Invalid;
        }
        Data.MethodDescVersioningState methodDescVersioningStateData = _target.ProcessedData.GetOrAdd<Data.MethodDescVersioningState>(methodDescVersioningStateAddress);
        // CodeVersionManager::GetNativeCodeVersion(PTR_MethodDesc, PCODE startAddress)
        return FindFirstCodeVersion(methodDescVersioningStateData, (codeVersion) =>
        {
            return codeVersion.MethodDesc == md.Address && codeVersion.NativeCode == startAddress;
        });
    }

    private NativeCodeVersionHandle FindFirstCodeVersion(Data.MethodDescVersioningState versioningState, Func<Data.NativeCodeVersionNode, bool> predicate)
    {
        // NativeCodeVersion::Next, heavily inlined
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
        // CodeVersionManager::GetActiveILCodeVersion
        // then ILCodeVersion::GetActiveNativeCodeVersion
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
```

**FIXME**

### Determining whether a method descriptor supports code versioning

**TODO**
