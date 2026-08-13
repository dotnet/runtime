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

public struct SimpleComCallWrapperData
{
    public ulong RefCount;
    public bool IsNeutered;
    public bool IsAggregated;
    public bool IsExtendsCOMObject;
    public bool IsHandleWeak;
    public TargetPointer OuterIUnknown;
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

// Resolves a COM interface pointer to the ComCallWrapper.
// Returns TargetPointer.Null if interfacePointer is not a recognised COM interface pointer.
// Use GetStartWrapper on the result to navigate to the start of the chain.
public TargetPointer GetCCWFromInterfacePointer(TargetPointer interfacePointer);
// Enumerates COM interfaces exposed by the ComCallWrapper chain.
// ccw may be any ComCallWrapper in the chain; the implementation navigates to the start.
public IEnumerable<COMInterfacePointerData> GetCCWInterfaces(TargetPointer ccw);
// Returns the GC object handle from the given ComCallWrapper.
public TargetPointer GetObjectHandle(TargetPointer ccw);
// Returns the data stored in the SimpleComCallWrapper associated with the given ComCallWrapper.
public SimpleComCallWrapperData GetSimpleComCallWrapperData(TargetPointer ccw);
// Navigates to the start ComCallWrapper in a linked chain.
// If ccw is already the start wrapper (or the only wrapper), returns ccw unchanged.
public TargetPointer GetStartWrapper(TargetPointer ccw);
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

<!-- BEGIN GENERATED: usage contract=BuiltInCOM version=c1 -->
### Data descriptors used

| Data Descriptor | Field | Type | Meaning |
| --- | --- | --- | --- |
| `ComCallWrapper` | `Handle` | `pointer` | GC object handle (m_ppThis); dereference to get the managed object pointer |
| `ComCallWrapper` | `IPtr` | `pointer` | Base address of the COM interface pointer array |
| `ComCallWrapper` | `Next` | `pointer` | Next wrapper in the linked chain (all-bits-set sentinel = end of list) |
| `ComCallWrapper` | `SimpleWrapper` | `pointer` | Address of the associated SimpleComCallWrapper |
| `ComMethodTable` | *(type size)* | `uint32` | Size in bytes of the fixed ComMethodTable header immediately preceding its vtable |
| `ComMethodTable` | `Flags` | `nuint` | Flags word; bit 0x10 (LayoutComplete) must be set for valid vtable |
| `ComMethodTable` | `MethodTable` | `pointer` | Pointer to the managed MethodTable for this COM interface |
| `CtxEntry` | `CtxCookie` | `pointer` | Context cookie stored in the context entry; compared against the RCW's cookie to detect disconnection |
| `CtxEntry` | `STAThread` | `pointer` | STA thread pointer for the context entry |
| `InterfaceEntry` | *(type size)* | `uint32` | Size in bytes of each entry in an RCW's inline interface cache |
| `InterfaceEntry` | `MethodTable` | `pointer` | MethodTable pointer for the cached COM interface |
| `InterfaceEntry` | `Unknown` | `pointer` | IUnknown* pointer for the cached COM interface |
| `RCW` | `CreatorThread` | `pointer` | Pointer to the thread that created this RCW |
| `RCW` | `CtxCookie` | `pointer` | COM context cookie for the RCW |
| `RCW` | `CtxEntry` | `pointer` | Pointer to CtxEntry (bit 0 is a synchronization flag; must be masked off before use) |
| `RCW` | `Flags` | `uint32` | Combined flags DWORD (contains marshaling type, aggregation, and containment bits) |
| `RCW` | `IdentityPointer` | `pointer` | Identity IUnknown* used to identify the underlying COM object |
| `RCW` | `InterfaceEntries` | `pointer` | Offset of the inline interface entry cache array within the RCW struct |
| `RCW` | `NextCleanupBucket` | `pointer` | Next bucket in the cleanup list |
| `RCW` | `NextRCW` | `pointer` | Next RCW in the same bucket |
| `RCW` | `RefCount` | `uint32` | Reference count of the RCW wrapper |
| `RCW` | `SyncBlockIndex` | `uint32` | Index into the sync block table; used to resolve the managed object (0 = no managed object) |
| `RCW` | `UnknownPointer` | `pointer` | Primary IUnknown* pointer for the RCW; a sentinel value indicates disconnection |
| `RCW` | `VTablePtr` | `pointer` | Vtable pointer of the COM object |
| `RCWCleanupList` | `FirstBucket` | `pointer` | Head of the bucket linked list |
| `SimpleComCallWrapper` | `Flags` | `uint32` | Bit flags for wrapper properties (aggregated, extends-COM, handle-weak, etc.) |
| `SimpleComCallWrapper` | `MainWrapper` | `pointer` | Pointer back to the first (start) ComCallWrapper in the chain |
| `SimpleComCallWrapper` | `OuterIUnknown` | `pointer` | Outer IUnknown pointer for aggregated CCWs (m_pOuter) |
| `SimpleComCallWrapper` | `RefCount` | `int64` | The wrapper refcount value (includes CLEANUP_SENTINEL bit) |
| `SimpleComCallWrapper` | `VTablePtr` | `pointer` | Base address of the standard interface vtable pointer array (used for SCCW IP resolution) |

### Global variables used

| Global | Type | Meaning |
| --- | --- | --- |
| `CCWNumInterfaces` | `uint32` | Number of vtable pointer slots in each ComCallWrapper (NumVtablePtrs = 5) |
| `CCWThisMask` | `pointer` | Alignment mask applied to a standard CCW IP to recover the ComCallWrapper pointer |
| `RCWCleanupList` | `pointer` | Pointer to the global g_pRCWCleanupList instance |
| `RCWInterfaceCacheSize` | `uint32` | Number of entries in the inline interface entry cache (INTERFACE_ENTRY_CACHE_SIZE) |
| `TearOffAddRef` | `pointer` | Address of Unknown_AddRef; identifies standard CCW interface pointers |
| `TearOffAddRefSimple` | `pointer` | Address of Unknown_AddRefSpecial; identifies SimpleComCallWrapper interface pointers |
| `TearOffAddRefSimpleInner` | `pointer` | Address of Unknown_AddRefInner; identifies inner SimpleComCallWrapper interface pointers |

### Contracts used

| Contract Name |
| --- |
| `SyncBlock` |
<!-- END GENERATED: usage contract=BuiltInCOM version=c1 -->


### Contract Constants:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `DisconnectedSentinel` | `ulong` | Sentinel value written to the unknown pointer when an RCW is disconnected | `0xBADF00D` |


``` csharp

// Mirrors enum SimpleComCallWrapperFlags in src/coreclr/vm/comcallablewrapper.h
private enum SimpleComCallWrapperFlags : uint
{
    IsAggregated  = 0x1,
    IsExtendsCom  = 0x2,
    IsHandleWeak  = 0x4,
}
// Mirrors enum Masks in src/coreclr/vm/comcallablewrapper.h
private enum ComMethodTableFlags : ulong
{
    LayoutComplete = 0x10,
}

[Flags]
private enum ComRefCount : long
{
    ComRefCountMask           = 0x000000007FFFFFFFL,
    CleanupSentinel          = 0x80000000L,
}

[Flags]
private enum RCWFlags : uint
{
    URTAggregated          = 0x010u,
    URTContained           = 0x020u,
    MarshalingTypeMask     = 0x180u,
    MarshalingTypeFreeThreaded = 0x100u,
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

// Returns the GC object handle from the given ComCallWrapper.
public TargetPointer GetObjectHandle(TargetPointer ccw)
    => _target.ReadPointer(ccw + /* ComCallWrapper::Handle offset */);

// Returns data from the SimpleComCallWrapper associated with the given ComCallWrapper.
// Applies ComRefCountMask to produce the visible RefCount and checks CLEANUP_SENTINEL for IsNeutered.
public SimpleComCallWrapperData GetSimpleComCallWrapperData(TargetPointer ccw)
{
    TargetPointer sccw = _target.ReadPointer(ccw + /* ComCallWrapper::SimpleWrapper offset */);
    ulong rawRefCount = _target.Read<ulong>(sccw + /* SimpleComCallWrapper::RefCount offset */);
    uint flags = _target.Read<uint>(sccw + /* SimpleComCallWrapper::Flags offset */);
    return new SimpleComCallWrapperData
    {
        RefCount         = rawRefCount & ComRefCount.ComRefCountMask,
        IsNeutered       = (rawRefCount & ComRefCount.CleanupSentinel) != 0,
        IsAggregated     = (flags & (uint)SimpleComCallWrapperFlags.IsAggregated) != 0,
        IsExtendsCOMObject = (flags & (uint)SimpleComCallWrapperFlags.IsExtendsCom) != 0,
        IsHandleWeak     = (flags & (uint)SimpleComCallWrapperFlags.IsHandleWeak) != 0,
        OuterIUnknown    = _target.ReadPointer(sccw + /* SimpleComCallWrapper::OuterIUnknown offset */),
    };
}

// Navigates to the start ComCallWrapper in a linked chain.
// If ccw is already the start wrapper (or the only wrapper), returns ccw unchanged.
public TargetPointer GetStartWrapper(TargetPointer ccw)
{
    TargetPointer next = _target.ReadPointer(ccw + /* ComCallWrapper::Next offset */);
    if (next != Null)
    {
        TargetPointer sccw = _target.ReadPointer(ccw + /* ComCallWrapper::SimpleWrapper offset */);
        ccw = _target.ReadPointer(sccw + /* SimpleComCallWrapper::MainWrapper offset */);
    }
    return ccw;
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

