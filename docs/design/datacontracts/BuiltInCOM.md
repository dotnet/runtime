# Contract BuiltInCOM

This contract is for getting information related to built-in COM.

## APIs of contract

``` csharp
public struct COMInterfacePointerData
{
    // Address of the slot in ComCallWrapper that holds the COM interface pointer.
    public TargetPointer InterfacePointerAddress;
    // MethodTable for this interface, or TargetPointer.Null for slot 0 (IUnknown/IDispatch).
    public TargetPointer MethodTable;
}

public record struct RCWCleanupInfo(
    TargetPointer RCW,
    TargetPointer Context,
    TargetPointer STAThread,
    bool IsFreeThreaded);

public record struct RCWData(
    TargetPointer IdentityPointer,
    TargetPointer UnknownPointer,
    TargetPointer ManagedObject,
    TargetPointer VTablePtr,
    TargetPointer CreatorThread,
    TargetPointer CtxCookie,
    uint RefCount,
    bool IsAggregated,
    bool IsContained,
    bool IsFreeThreaded,
    bool IsDisconnected);

public ulong GetRefCount(TargetPointer ccw);
// Check whether the COM wrappers handle is weak.
public bool IsHandleWeak(TargetPointer ccw);
// Resolves a COM interface pointer to the ComCallWrapper.
// Returns TargetPointer.Null if interfacePointer is not a recognised COM interface pointer.
public TargetPointer GetCCWFromInterfacePointer(TargetPointer interfacePointer);
// Enumerate the COM interfaces exposed by the ComCallWrapper chain.
// ccw may be any ComCallWrapper in the chain; the implementation navigates to the start.
public IEnumerable<COMInterfacePointerData> GetCCWInterfaces(TargetPointer ccw);
// Enumerate entries in the RCW cleanup list.
// If cleanupListPtr is Null, the global g_pRCWCleanupList is used.
public IEnumerable<RCWCleanupInfo> GetRCWCleanupList(TargetPointer cleanupListPtr);
// Enumerate the interface entries cached in an RCW.
public IEnumerable<(TargetPointer MethodTable, TargetPointer Unknown)> GetRCWInterfaces(TargetPointer rcw);
// Get the COM context cookie for an RCW.
public TargetPointer GetRCWContext(TargetPointer rcw);
// Get detailed data about an RCW, including flags and the managed object reference.
public RCWData GetRCWData(TargetPointer rcw);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `ComCallWrapper` | `SimpleWrapper` | Address of the associated `SimpleComCallWrapper` |
| `ComCallWrapper` | `IPtr` | Base address of the COM interface pointer array |
| `ComCallWrapper` | `Next` | Next wrapper in the linked chain (all-bits-set sentinel = end of list) |
| `ComMethodTable` | `Flags` | Flags word; bit `0x10` (`LayoutComplete`) must be set for valid vtable |
| `ComMethodTable` | `MethodTable` | Pointer to the managed `MethodTable` for this COM interface |
| `SimpleComCallWrapper` | `RefCount` | The wrapper refcount value |
| `SimpleComCallWrapper` | `Flags` | Bit flags for wrapper properties |
| `SimpleComCallWrapper` | `MainWrapper` | Pointer back to the first (start) `ComCallWrapper` in the chain |
| `SimpleComCallWrapper` | `VTablePtr` | Base address of the standard interface vtable pointer array (used for SCCW IP resolution) |
| `RCWCleanupList` | `FirstBucket` | Head of the bucket linked list |
| `RCW` | `NextCleanupBucket` | Next bucket in the cleanup list |
| `RCW` | `NextRCW` | Next RCW in the same bucket |
| `RCW` | `Flags` | Combined flags DWORD (contains marshaling type, aggregation, and containment bits) |
| `RCW` | `CtxCookie` | COM context cookie for the RCW |
| `RCW` | `CtxEntry` | Pointer to `CtxEntry` (bit 0 is a synchronization flag; must be masked off before use) |
| `RCW` | `IdentityPointer` | Identity `IUnknown*` used to identify the underlying COM object |
| `RCW` | `SyncBlockIndex` | Index into the sync block table; used to resolve the managed object (0 = no managed object) |
| `RCW` | `VTablePtr` | Vtable pointer of the COM object |
| `RCW` | `CreatorThread` | Pointer to the thread that created this RCW |
| `RCW` | `RefCount` | Reference count of the RCW wrapper |
| `RCW` | `UnknownPointer` | Primary `IUnknown*` pointer for the RCW; a sentinel value indicates disconnection |
| `RCW` | `InterfaceEntries` | Offset of the inline interface entry cache array within the RCW struct |
| `CtxEntry` | `STAThread` | STA thread pointer for the context entry |
| `CtxEntry` | `CtxCookie` | Context cookie stored in the context entry; compared against the RCW's cookie to detect disconnection |
| `InterfaceEntry` | `MethodTable` | MethodTable pointer for the cached COM interface |
| `InterfaceEntry` | `Unknown` | `IUnknown*` pointer for the cached COM interface |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `ComRefcountMask` | `long` | Mask applied to `SimpleComCallWrapper.RefCount` to produce the visible refcount |
| `CCWNumInterfaces` | `uint` | Number of vtable pointer slots in each `ComCallWrapper` (`NumVtablePtrs = 5`) |
| `CCWThisMask` | `nuint` | Alignment mask applied to a standard CCW IP to recover the `ComCallWrapper` pointer |
| `TearOffAddRef` | pointer | Address of `Unknown_AddRef`; identifies standard CCW interface pointers |
| `TearOffAddRefSimple` | pointer | Address of `Unknown_AddRefSpecial`; identifies `SimpleComCallWrapper` interface pointers |
| `TearOffAddRefSimpleInner` | pointer | Address of `Unknown_AddRefInner`; identifies inner `SimpleComCallWrapper` interface pointers |
| `RCWCleanupList` | `pointer` | Pointer to the global `g_pRCWCleanupList` instance |
| `RCWInterfaceCacheSize` | `uint32` | Number of entries in the inline interface entry cache (`INTERFACE_ENTRY_CACHE_SIZE`) |

### Contract Constants:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `DisconnectedSentinel` | `ulong` | Sentinel value written to the unknown pointer when an RCW is disconnected | `0xBADF00D` |

Contracts used:
| Contract Name |
| --- |
| `SyncBlock` |

``` csharp

private enum CCWFlags
{
    IsHandleWeak = 0x4,
}

private enum ComMethodTableFlags : ulong
{
    LayoutComplete = 0x10,
}

[Flags]
private enum RCWFlags : uint
{
    URTAggregated          = 0x010u,
    URTContained           = 0x020u,
    MarshalingTypeMask     = 0x180u,
    MarshalingTypeFreeThreaded = 0x100u,
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
    return (flags & (uint)CCWFlags.IsHandleWeak) != 0;
}

// See ClrDataAccess::DACGetCCWFromAddress in src/coreclr/debug/daccess/request.cpp.
// Resolves a COM interface pointer to the ComCallWrapper.
// Returns TargetPointer.Null if interfacePointer is not a recognised COM IP.
public TargetPointer GetCCWFromInterfacePointer(TargetPointer interfacePointer) { ... }

public IEnumerable<COMInterfacePointerData> GetCCWInterfaces(TargetPointer ccw)
{
    // Navigate to the start of the linked chain (ccw may be any wrapper in the chain).
    // Walk the linked list of ComCallWrapper nodes starting at the start wrapper.
    // For each node, iterate the IPtrs[] slots:
    //   - skip null slots
    //   - skip slots where ComMethodTable.Flags does not have LayoutComplete set
    //   - yield COMInterfacePointerData { InterfacePointerAddress = address of slot, MethodTable }
    //   - slot 0 of the first wrapper (IUnknown/IDispatch) yields null MethodTable
}

public IEnumerable<RCWCleanupInfo> GetRCWCleanupList(TargetPointer cleanupListPtr)
{
    // Resolve the cleanup list address.
    TargetPointer listAddress = cleanupListPtr != TargetPointer.Null
        ? cleanupListPtr
        : _target.ReadPointer(_target.ReadGlobalPointer("RCWCleanupList"));

    if (listAddress == TargetPointer.Null)
        yield break;

    // Walk bucket chain. Each bucket is a linked list of RCWs sharing the same context.
    TargetPointer bucketPtr = _target.ReadPointer(listAddress + /* RCWCleanupList::FirstBucket offset */);
    while (bucketPtr != TargetPointer.Null)
    {
        uint flags = _target.Read<uint>(bucketPtr + /* RCW::Flags offset */);
        bool isFreeThreaded = ((RCWFlags)flags & RCWFlags.MarshalingTypeMask) == RCWFlags.MarshalingTypeFreeThreaded;
        TargetPointer ctxCookie = _target.ReadPointer(bucketPtr + /* RCW::CtxCookie offset */);

        // CtxEntry uses bit 0 for synchronization; strip it before dereferencing.
        TargetPointer ctxEntry = _target.ReadPointer(bucketPtr + /* RCW::CtxEntry offset */) & ~(ulong)1;
        TargetPointer staThread = ctxEntry != TargetPointer.Null
            ? _target.ReadPointer(ctxEntry + /* CtxEntry::STAThread offset */)
            : TargetPointer.Null;

        TargetPointer rcwPtr = bucketPtr;
        while (rcwPtr != TargetPointer.Null)
        {
            yield return new RCWCleanupInfo(rcwPtr, ctxCookie, staThread, isFreeThreaded);
            rcwPtr = _target.ReadPointer(rcwPtr + /* RCW::NextRCW offset */);
        }

        bucketPtr = _target.ReadPointer(bucketPtr + /* RCW::NextCleanupBucket offset */);
    }
}

public IEnumerable<(TargetPointer MethodTable, TargetPointer Unknown)> GetRCWInterfaces(TargetPointer rcw)
{
    // InterfaceEntries is an inline array — the offset gives the address of the first element.
    TargetPointer interfaceEntriesAddr = rcw + /* RCW::InterfaceEntries offset */;
    uint cacheSize = _target.ReadGlobal<uint>("RCWInterfaceCacheSize");
    uint entrySize = /* size of InterfaceEntry */;

    for (uint i = 0; i < cacheSize; i++)
    {
        TargetPointer entryAddress = interfaceEntriesAddr + i * entrySize;
        TargetPointer methodTable = _target.ReadPointer(entryAddress + /* InterfaceEntry::MethodTable offset */);
        TargetPointer unknown = _target.ReadPointer(entryAddress + /* InterfaceEntry::Unknown offset */);
        // An entry is free if Unknown == null (matches InterfaceEntry::IsFree())
        if (unknown != TargetPointer.Null)
            yield return (methodTable, unknown);
    }
}

public TargetPointer GetRCWContext(TargetPointer rcw)
{
    return _target.ReadPointer(rcw + /* RCW::CtxCookie offset */);
}

public RCWData GetRCWData(TargetPointer rcw)
{
    TargetPointer managedObject = TargetPointer.Null;
    uint syncBlockIndex = _target.Read<uint>(rcw + /* RCW::SyncBlockIndex offset */);
    if (syncBlockIndex != 0)
    {
        managedObject = _target.Contracts.SyncBlock.GetSyncBlockObject(syncBlockIndex);
    }

    uint flags = _target.Read<uint>(rcw + /* RCW::Flags offset */);

    return new RCWData(
        IdentityPointer: _target.ReadPointer(rcw + /* RCW::IdentityPointer offset */),
        UnknownPointer: _target.ReadPointer(rcw + /* RCW::UnknownPointer offset */),
        ManagedObject: managedObject,
        VTablePtr: _target.ReadPointer(rcw + /* RCW::VTablePtr offset */),
        CreatorThread: _target.ReadPointer(rcw + /* RCW::CreatorThread offset */),
        CtxCookie: _target.ReadPointer(rcw + /* RCW::CtxCookie offset */),
        RefCount: _target.Read<uint>(rcw + /* RCW::RefCount offset */),
        IsAggregated: ((RCWFlags)flags).HasFlag(RCWFlags.URTAggregated),
        IsContained: ((RCWFlags)flags).HasFlag(RCWFlags.URTContained),
        IsFreeThreaded: ((RCWFlags)flags & RCWFlags.MarshalingTypeMask) == RCWFlags.MarshalingTypeFreeThreaded,
        IsDisconnected: IsRCWDisconnected(rcw));
}

// An RCW is disconnected if its unknown pointer holds the sentinel value,
// or if its context cookie no longer matches the cookie stored in its context entry.
private bool IsRCWDisconnected(TargetPointer rcw)
{
    TargetPointer unknownPointer = _target.ReadPointer(rcw + /* RCW::UnknownPointer offset */);
    if (unknownPointer == DisconnectedSentinel)
        return true;

    TargetPointer ctxEntryPtr = _target.ReadPointer(rcw + /* RCW::CtxEntry offset */) & ~(ulong)1;
    if (ctxEntryPtr == TargetPointer.Null)
        return false;

    TargetPointer rcwCookie = _target.ReadPointer(rcw + /* RCW::CtxCookie offset */);
    TargetPointer entryCookie = _target.ReadPointer(ctxEntryPtr + /* CtxEntry::CtxCookie offset */);
    return rcwCookie != entryCookie;
}
```

