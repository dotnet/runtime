# Contract BuiltInCOM

This contract is for getting information related to built-in COM.

## APIs of contract

``` csharp
public ulong GetRefCount(TargetPointer ccw);
// Check whether the COM wrapper's handle is weak.
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
// Returns the address of the start ComCallWrapper for the given CCW address.
// All wrappers in a chain share the same SimpleComCallWrapper, so any wrapper address is accepted.
public TargetPointer GetCCWAddress(TargetPointer ccw);
// Returns the GC object handle (m_ppThis) of the start ComCallWrapper.
public TargetPointer GetCCWHandle(TargetPointer ccw);
// Returns the outer IUnknown pointer (m_pOuter) for aggregated CCWs.
// All wrappers in a chain share the same SimpleComCallWrapper, so any wrapper address is accepted.
public TargetPointer GetOuterIUnknown(TargetPointer ccw);
// Returns the address of the SimpleComCallWrapper associated with the given ComCallWrapper.
public TargetPointer GetSimpleComCallWrapper(TargetPointer ccw);
// Returns the data stored in a SimpleComCallWrapper.
// sccw must be a SimpleComCallWrapper address (obtain via GetSimpleComCallWrapper).
public SimpleComCallWrapperData GetSimpleComCallWrapperData(TargetPointer sccw);
// Enumerate entries in the RCW cleanup list.
// If cleanupListPtr is Null, the global g_pRCWCleanupList is used.
public IEnumerable<RCWCleanupInfo> GetRCWCleanupList(TargetPointer cleanupListPtr);
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

and `SimpleComCallWrapperData` is:
``` csharp
public struct SimpleComCallWrapperData
{
    public ulong RefCount;       // visible refcount (raw refcount with CLEANUP_SENTINEL and other non-count bits masked off)
    public bool IsNeutered;      // true if CLEANUP_SENTINEL bit was set in the raw ref count
    public uint Flags;           // IsAggregated = 0x1, IsExtendsCom = 0x2, IsHandleWeak = 0x4
    public TargetPointer OuterIUnknown;  // outer IUnknown for aggregation (m_pOuter)
    public TargetPointer MainWrapper;    // start ComCallWrapper in the chain
}
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

// Returns the address of the SimpleComCallWrapper associated with the given ComCallWrapper.
public TargetPointer GetSimpleComCallWrapper(TargetPointer ccw)
    => _target.ReadPointer(ccw + /* ComCallWrapper::SimpleWrapper offset */);

// Returns data from the SimpleComCallWrapper at sccw.
// Applies ComRefcountMask to produce the visible RefCount and checks CLEANUP_SENTINEL for IsNeutered.
public SimpleComCallWrapperData GetSimpleComCallWrapperData(TargetPointer sccw)
{
    ulong rawRefCount = _target.Read<ulong>(sccw + /* SimpleComCallWrapper::RefCount offset */);
    long refCountMask = _target.ReadGlobal<long>("ComRefcountMask");
    return new SimpleComCallWrapperData
    {
        RefCount      = rawRefCount & (ulong)refCountMask,
        IsNeutered    = (rawRefCount & CleanupSentinel) != 0,
        Flags         = _target.Read<uint>(sccw + /* SimpleComCallWrapper::Flags offset */),
        OuterIUnknown = _target.ReadPointer(sccw + /* SimpleComCallWrapper::OuterIUnknown offset */),
        MainWrapper   = _target.ReadPointer(sccw + /* SimpleComCallWrapper::MainWrapper offset */),
    };
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

// Returns the address of the start ComCallWrapper.
// Unconditionally follows SimpleWrapper→MainWrapper (mirrors ComCallWrapper::GetStartWrapper).
public TargetPointer GetCCWAddress(TargetPointer ccw)
    => GetSimpleComCallWrapperData(GetSimpleComCallWrapper(ccw)).MainWrapper;

// Returns the GC object handle (m_ppThis) of the start ComCallWrapper.
public TargetPointer GetCCWHandle(TargetPointer ccw)
{
    TargetPointer startCCW = GetCCWAddress(ccw);
    return _target.ReadPointer(startCCW + /* ComCallWrapper::Handle offset */);
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

