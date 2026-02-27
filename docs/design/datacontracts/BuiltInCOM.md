# Contract BuiltInCOM

This contract is for getting information related to built-in COM.

## APIs of contract

``` csharp
public ulong GetRefCount(TargetPointer ccw);
// Check whether the COM wrappers handle is weak.
public bool IsHandleWeak(TargetPointer ccw);
// Enumerate entries in the RCW cleanup list.
// If cleanupListPtr is Null, the global g_pRCWCleanupList is used.
public IEnumerable<RCWCleanupInfo> GetRCWCleanupList(TargetPointer cleanupListPtr);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `ComCallWrapper` | `SimpleWrapper` | Address of the associated `SimpleComCallWrapper` |
| `SimpleComCallWrapper` | `RefCount` | The wrapper refcount value |
| `SimpleComCallWrapper` | `Flags` | Bit flags for wrapper properties |
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
| `RCWCleanupList` | `pointer` | Pointer to the global `g_pRCWCleanupList` instance |

### Contract Constants:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `MarshalingTypeShift` | `int` | Bit position of `m_MarshalingType` within `RCW::RCWFlags::m_dwFlags` | `7` |

Contracts used:
| Contract Name |
| --- |
`None`

``` csharp

private enum Flags
{
    IsHandleWeak = 0x4,
}

// MarshalingTypeShift = 7 matches the bit position of m_MarshalingType in RCW::RCWFlags::m_dwFlags
private const int MarshalingTypeShift = 7;
private const uint MarshalingTypeMask = 0x3u << MarshalingTypeShift;
private const uint MarshalingTypeFreeThreaded = 2u << MarshalingTypeShift; // matches RCW::MarshalingType_FreeThreaded

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
        bool isFreeThreaded = (flags & MarshalingTypeMask) == MarshalingTypeFreeThreaded;
        TargetPointer ctxCookie = _target.ReadPointer(bucketPtr + /* RCW::CtxCookie offset */);

        // m_pCtxEntry uses bit 0 for synchronization; strip it before dereferencing.
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
