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

// Resolves a COM interface pointer to a ComCallWrapper in the chain.
// Returns TargetPointer.Null if interfacePointer is not a recognised COM interface pointer.
// Use GetStartWrapper on the result to navigate to the start of the chain.
public TargetPointer GetCCWFromInterfacePointer(TargetPointer interfacePointer);
// Enumerates COM interfaces exposed by the ComCallWrapper chain.
// ccw may be any ComCallWrapper in the chain; the implementation navigates to the start.
public IEnumerable<COMInterfacePointerData> GetCCWInterfaces(TargetPointer ccw);
// Returns the GC object handle from the given ComCallWrapper.
public TargetPointer GetObjectHandle(TargetPointer ccw);
// Returns the address of the SimpleComCallWrapper associated with the given ComCallWrapper.
public TargetPointer GetSimpleComCallWrapper(TargetPointer ccw);
// Returns the data stored in a SimpleComCallWrapper.
// sccw must be a SimpleComCallWrapper address (obtain via GetSimpleComCallWrapper).
public SimpleComCallWrapperData GetSimpleComCallWrapperData(TargetPointer sccw);
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
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `ComCallWrapper` | `Handle` | GC object handle (`m_ppThis`); dereference to get the managed object pointer |
| `ComCallWrapper` | `SimpleWrapper` | Address of the associated `SimpleComCallWrapper` |
| `ComCallWrapper` | `IPtr` | Base address of the COM interface pointer array |
| `ComCallWrapper` | `Next` | Next wrapper in the linked chain (all-bits-set sentinel = end of list) |
| `ComMethodTable` | `Flags` | Flags word; bit `0x10` (`LayoutComplete`) must be set for valid vtable |
| `ComMethodTable` | `MethodTable` | Pointer to the managed `MethodTable` for this COM interface |
| `SimpleComCallWrapper` | `OuterIUnknown` | Outer `IUnknown` pointer for aggregated CCWs (`m_pOuter`) |
| `SimpleComCallWrapper` | `RefCount` | The wrapper refcount value (includes `CLEANUP_SENTINEL` bit) |
| `SimpleComCallWrapper` | `Flags` | Bit flags for wrapper properties (aggregated, extends-COM, handle-weak, etc.) |
| `SimpleComCallWrapper` | `MainWrapper` | Pointer back to the first (start) `ComCallWrapper` in the chain |
| `SimpleComCallWrapper` | `VTablePtr` | Base address of the standard interface vtable pointer array (used for SCCW IP resolution) |
| `RCWCleanupList` | `FirstBucket` | Head of the bucket linked list |
| `RCW` | `NextCleanupBucket` | Next bucket in the cleanup list |
| `RCW` | `NextRCW` | Next RCW in the same bucket |
| `RCW` | `Flags` | Combined flags DWORD (contains `MarshalingType` bits) |
| `RCW` | `CtxCookie` | COM context cookie for the RCW |
| `RCW` | `CtxEntry` | Pointer to `CtxEntry` (bit 0 is a synchronization flag; must be masked off before use) |
| `CtxEntry` | `STAThread` | STA thread pointer for the context entry |
| `RCW` | `InterfaceEntries` | Offset of the inline interface entry cache array within the RCW struct |
| `InterfaceEntry` | `MethodTable` | MethodTable pointer for the cached COM interface |
| `InterfaceEntry` | `Unknown` | `IUnknown*` pointer for the cached COM interface |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
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
| `MarshalingTypeShift` | `int` | Bit position of `m_MarshalingType` within `RCW::RCWFlags::m_dwFlags` | `7` |
| `MarshalingTypeFreeThreaded` | `int` | Enum value for marshaling type within `RCW::RCWFlags::m_dwFlags` | `2` |
| `CleanupSentinel` | `ulong` | Bit 31 of `SimpleComCallWrapper.RefCount`; set when the CCW is neutered | `0x80000000` |
| `ComRefCountMask` | `ulong` | Mask applied to `SimpleComCallWrapper.RefCount` to produce the visible refcount | `0x7FFFFFFF` |

Contracts used:
| Contract Name |
| --- |
`None`

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
// MarshalingTypeShift = 7 matches the bit position of m_MarshalingType in RCW::RCWFlags::m_dwFlags
private const int MarshalingTypeShift = 7;
private const uint MarshalingTypeMask = 0x3u << MarshalingTypeShift;
private const uint MarshalingTypeFreeThreaded = 2u; // matches RCW::MarshalingType_FreeThreaded
// CLEANUP_SENTINEL: bit 31 of m_llRefCount; set when the CCW is neutered
private const ulong CleanupSentinel = 0x80000000UL;
// COM_REFCOUNT_MASK: lower 31 bits of m_llRefCount hold the visible refcount
private const ulong ComRefCountMask = 0x000000007FFFFFFFUL;

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

// Returns the address of the SimpleComCallWrapper associated with the given ComCallWrapper.
public TargetPointer GetSimpleComCallWrapper(TargetPointer ccw)
    => _target.ReadPointer(ccw + /* ComCallWrapper::SimpleWrapper offset */);

// Returns data from the SimpleComCallWrapper at sccw.
// Applies ComRefCountMask to produce the visible RefCount and checks CLEANUP_SENTINEL for IsNeutered.
public SimpleComCallWrapperData GetSimpleComCallWrapperData(TargetPointer sccw)
{
    ulong rawRefCount = _target.Read<ulong>(sccw + /* SimpleComCallWrapper::RefCount offset */);
    uint flags = _target.Read<uint>(sccw + /* SimpleComCallWrapper::Flags offset */);
    return new SimpleComCallWrapperData
    {
        RefCount         = rawRefCount & ComRefCountMask,
        IsNeutered       = (rawRefCount & CleanupSentinel) != 0,
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
        bool isFreeThreaded = (flags & MarshalingTypeMask) == MarshalingTypeFreeThreaded << MarshalingTypeShift;
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
```

