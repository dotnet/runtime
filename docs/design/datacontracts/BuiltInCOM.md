# Contract BuiltInCOM

This contract is for getting information related to built-in COM.

## APIs of contract

``` csharp
public ulong GetRefCount(TargetPointer ccw);
// Check whether the COM wrappers handle is weak.
public bool IsHandleWeak(TargetPointer ccw);
// Resolves a COM interface pointer to the ComCallWrapper.
// Returns TargetPointer.Null if interfacePointer is not a recognised COM interface pointer.
public TargetPointer GetCCWFromInterfacePointer(TargetPointer interfacePointer);
// Enumerate the COM interfaces exposed by the ComCallWrapper chain.
// ccw may be any ComCallWrapper in the chain; the implementation navigates to the start.
public IEnumerable<COMInterfacePointerData> GetCCWInterfaces(TargetPointer ccw);
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
// Enumerate entries in the RCW cleanup list.
// If cleanupListPtr is Null, the global g_pRCWCleanupList is used.
public IEnumerable<RCWCleanupInfo> GetRCWCleanupList(TargetPointer cleanupListPtr);
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

Contracts used:
| Contract Name |
| --- |
`None`

``` csharp

private enum CCWFlags
{
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

