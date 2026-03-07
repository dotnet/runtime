# Contract BuiltInCOM

This contract is for getting information related to built-in COM.

## APIs of contract

``` csharp
public ulong GetRefCount(TargetPointer ccw);
// Check whether the COM wrappers handle is weak.
public bool IsHandleWeak(TargetPointer ccw);
// Returns true if the CCW has been neutered (CLEANUP_SENTINEL bit set in the raw ref count).
public bool IsNeutered(TargetPointer ccw);
// Returns true if the managed class extends a COM object.
public bool IsExtendsCOMObject(TargetPointer ccw);
// Returns true if the CCW is aggregated.
public bool IsAggregated(TargetPointer ccw);
// Resolves a COM interface pointer to the ComCallWrapper.
// Returns TargetPointer.Null if interfacePointer is not a recognised COM interface pointer.
public TargetPointer GetCCWFromInterfacePointer(TargetPointer interfacePointer);
// Enumerate the COM interfaces exposed by the ComCallWrapper chain.
// ccw may be any ComCallWrapper in the chain; the implementation navigates to the start.
public IEnumerable<COMInterfacePointerData> GetCCWInterfaces(TargetPointer ccw);
// Returns aggregated CCW data for the given ComCallWrapper address.
// ccw must be a ComCallWrapper address (resolve COM interface pointers first via GetCCWFromInterfacePointer).
public CCWData GetCCWData(TargetPointer ccw);
```

where `COMInterfacePointerData` is:
``` csharp
public struct COMInterfacePointerData
{
    // Address of the slot in ComCallWrapper that holds the COM interface pointer.
    public TargetPointer InterfacePointerAddress;
    // MethodTable for this interface, or TargetPointer.Null for slot 0 (IUnknown/IDispatch).
    public TargetPointer MethodTable;
}
```

and `CCWData` is:
``` csharp
public struct CCWData
{
    public TargetPointer OuterIUnknown;      // outer IUnknown for aggregation (m_pOuter)
    public TargetPointer ManagedObject;      // managed object pointer (dereferenced from Handle)
    public TargetPointer Handle;             // GC handle holding the managed object (m_ppThis)
    public TargetPointer CCWAddress;         // address of the start ComCallWrapper
    public int RefCount;                     // COM reference count (masked with ComRefcountMask)
    public int InterfaceCount;              // number of exposed COM interfaces
    public bool IsNeutered;                  // true if the CLEANUP_SENTINEL bit is set
    public bool HasStrongRef;               // true if RefCount > 0 and handle is not weak
    public bool IsExtendsCOMObject;         // true if the managed class extends a COM object
    public bool IsAggregated;               // true if the CCW is aggregated
}
```

// Enumerate entries in the RCW cleanup list.
// If cleanupListPtr is Null, the global g_pRCWCleanupList is used.
public IEnumerable<RCWCleanupInfo> GetRCWCleanupList(TargetPointer cleanupListPtr);
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

### Contract Constants:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `MarshalingTypeShift` | `int` | Bit position of `m_MarshalingType` within `RCW::RCWFlags::m_dwFlags` | `7` |
| `MarshalingTypeFreeThreaded` | `int` | Enum value for marshaling type within `RCW::RCWFlags::m_dwFlags` | `2` |
| `CleanupSentinel` | `ulong` | Bit 31 of `SimpleComCallWrapper.RefCount`; set when the CCW is neutered | `0x80000000` |

Contracts used:
| Contract Name |
| --- |
`None`

``` csharp

private enum CCWFlags
{
    IsAggregated = 0x1,
    IsExtendsCom = 0x2,
    IsHandleWeak = 0x4,
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

public bool IsNeutered(TargetPointer address)
{
    var ccw = _target.ReadPointer(address + /* ComCallWrapper::SimpleWrapper offset */);
    ulong refCount = _target.Read<ulong>(ccw + /* SimpleComCallWrapper::RefCount offset */);
    return (refCount & CleanupSentinel) != 0;
}

public bool IsExtendsCOMObject(TargetPointer address)
{
    var ccw = _target.ReadPointer(address + /* ComCallWrapper::SimpleWrapper offset */);
    uint flags = _target.Read<uint>(ccw + /* SimpleComCallWrapper::Flags offset */);
    return (flags & (uint)CCWFlags.IsExtendsCom) != 0;
}

public bool IsAggregated(TargetPointer address)
{
    var ccw = _target.ReadPointer(address + /* ComCallWrapper::SimpleWrapper offset */);
    uint flags = _target.Read<uint>(ccw + /* SimpleComCallWrapper::Flags offset */);
    return (flags & (uint)CCWFlags.IsAggregated) != 0;
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

// See ClrDataAccess::GetCCWData in src/coreclr/debug/daccess/request.cpp.
// ccw must be a ComCallWrapper address; resolve COM interface pointers first via GetCCWFromInterfacePointer.
public CCWData GetCCWData(TargetPointer ccw)
{
    // Navigate to the start of the chain; then compose from the existing predicate APIs.
    TargetPointer startCCW = NavigateToStartWrapper(ccw);
    int refCount = (int)GetRefCount(startCCW);
    return new CCWData
    {
        OuterIUnknown = /* SimpleComCallWrapper::OuterIUnknown */,
        ManagedObject = /* *Handle (dereference the GC handle) */,
        Handle        = /* ComCallWrapper::Handle (m_ppThis) */,
        CCWAddress    = startCCW,
        RefCount      = refCount,
        InterfaceCount = GetCCWInterfaces(startCCW).Count(),
        IsNeutered    = IsNeutered(startCCW),
        HasStrongRef  = (refCount > 0) && !IsHandleWeak(startCCW),
        IsExtendsCOMObject = IsExtendsCOMObject(startCCW),
        IsAggregated  = IsAggregated(startCCW),
    };
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
```

