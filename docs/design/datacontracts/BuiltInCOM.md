# Contract BuiltInCOM

This contract is for getting information related to built-in COM.

## APIs of contract

``` csharp
public ulong GetRefCount(TargetPointer ccw);
// Check whether the COM wrappers handle is weak.
public bool IsHandleWeak(TargetPointer ccw);
// Enumerate the interface entries cached in an RCW.
public IEnumerable<(TargetPointer MethodTable, TargetPointer Unknown)> GetRCWInterfaces(TargetPointer rcw);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `ComCallWrapper` | `SimpleWrapper` | Address of the associated `SimpleComCallWrapper` |
| `SimpleComCallWrapper` | `RefCount` | The wrapper refcount value |
| `SimpleComCallWrapper` | `Flags` | Bit flags for wrapper properties |
| `RCW` | `InterfaceEntries` | Offset of the inline interface entry cache array within the RCW struct |
| `InterfaceEntry` | `MethodTable` | MethodTable pointer for the cached COM interface |
| `InterfaceEntry` | `Unknown` | `IUnknown*` pointer for the cached COM interface |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `ComRefcountMask` | `long` | Mask applied to `SimpleComCallWrapper.RefCount` to produce the visible refcount |
| `RCWInterfaceCacheSize` | `uint32` | Number of entries in the inline interface entry cache (`INTERFACE_ENTRY_CACHE_SIZE`) |

Contracts used:
| Contract Name |
| --- |
`None`

``` csharp

private enum Flags
{
    IsHandleWeak = 0x4,
}

public ulong GetRefCount(TargetPointer address)
{
    var ccw = _target.ReadPointer(address + /* ComCallWrapper::SimpleWrapper offset */);
    ulong refCount = _target.Read<ulong>(ccw + /* SimpleComCallWrapper::RefCount offset */);
    long refCountMask = _target.ReadGlobal<long>("ComRefcountMask");
    return refCount & (ulong)refCountMask;
}

public bool IsHandleWeak(TargetPointer address)
{
    var ccw = _target.ReadPointer(address + /* ComCallWrapper::SimpleWrapper offset */);
    uint flags = _target.Read<uint>(ccw + /* SimpleComCallWrapper::Flags offset */);
    return (flags & (uint)Flags.IsHandleWeak) != 0;
}

public IEnumerable<(TargetPointer MethodTable, TargetPointer Unknown)> GetRCWInterfaces(TargetPointer rcw)
{
    // InterfaceEntries is the address of the inline array (address + offset of m_aInterfaceEntries)
    var rcwData = /* read RCW: InterfaceEntries = address + offset */;
    uint cacheSize = _target.ReadGlobal<uint>("RCWInterfaceCacheSize");
    uint entrySize = /* size of InterfaceEntry */;

    for (uint i = 0; i < cacheSize; i++)
    {
        TargetPointer entryAddress = rcwData.InterfaceEntries + i * entrySize;
        var entry = /* read InterfaceEntry from entryAddress */;
        // An entry is free if Unknown == null (matches InterfaceEntry::IsFree())
        if (entry.Unknown != TargetPointer.Null)
            yield return (entry.MethodTable, entry.Unknown);
    }
}
```
