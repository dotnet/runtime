# Contract SyncBlock

This contract is for reading sync block table entries and lock state.

## APIs of contract

``` csharp
TargetPointer GetSyncBlock(uint index);
TargetPointer GetSyncBlockObject(uint index);
bool IsSyncBlockFree(uint index);
uint GetSyncBlockCount();
bool TryGetLockInfo(TargetPointer syncBlock, out uint owningThreadId, out uint recursion);
uint GetAdditionalThreadCount(TargetPointer syncBlock);
TargetPointer GetSyncBlockFromCleanupList();
TargetPointer GetNextSyncBlock(TargetPointer syncBlock);
bool GetBuiltInComData(TargetPointer syncBlock, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf);
```

## Version 1

### Data descriptors used

| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `SyncTableEntry` | `SyncBlock` | Pointer to the sync block for a sync table entry |
| `SyncTableEntry` | `Object` | Pointer to the object associated with a sync table entry |
| `SyncBlockCache` | `FreeSyncTableIndex` | One past the highest sync table entry index allocated |
| `SyncBlockCache` | `CleanupBlockList` | Head of the cleanup list (points to the first `SyncBlock` in the chain) |
| `SyncBlock` | `Lock` | Optional pointer to a `System.Threading.Lock` object payload |
| `SyncBlock` | `ThinLock` | Thin-lock state bits |
| `SyncBlock` | `LinkNext` | Head pointer for cleanup list link |
| `SyncBlock` | `InteropInfo` | Optional pointer to an `InteropSyncBlockInfo` for the sync block |
| `InteropSyncBlockInfo` | `RCW` | RCW pointer; bit 0 is a lock bit and must be masked off |
| `InteropSyncBlockInfo` | `CCW` | CCW pointer; sentinel value `0x1` means previously had a CCW (treat as null) |
| `InteropSyncBlockInfo` | `CCF` | COM class factory pointer; sentinel value `0x1` means previously had a CCF (treat as null) |

### Global variables used

| Global Name | Type | Purpose |
| --- | --- | --- |
| `SyncTableEntries` | TargetPointer | Pointer to the sync table entries array |
| `SyncBlockCache` | TargetPointer | Pointer to the runtime sync block cache |
| `SyncBlockMaskLockThreadId` | uint32 | Mask for extracting thread id from `SyncBlock.ThinLock` |
| `SyncBlockMaskLockRecursionLevel` | uint32 | Mask for extracting recursion level from `SyncBlock.ThinLock` |
| `SyncBlockRecursionLevelShift` | uint32 | Shift value for `SyncBlock.ThinLock` recursion level |

### Managed types used

| Fully-qualified name | Module | Members read | Purpose |
| --- | --- | --- | --- |
| `System.Threading.Lock` | `System.Private.CoreLib` | `_state`, `_owningThreadId`, `_recursionCount` | Monitor-held state, owning thread id, and recursion count for fat-lock sync blocks |

### Contracts used

| Contract Name |
| --- |
| `ManagedTypeSource` |

``` csharp
TargetPointer GetSyncBlock(uint index)
{
    TargetPointer syncTableEntries = target.ReadGlobalPointer("SyncTableEntries");
    ulong offsetInSyncTable = index * /* SyncTableEntry size */;
    return target.ReadPointer(syncTableEntries + offsetInSyncTable + /* SyncTableEntry::SyncBlock offset */);
}

TargetPointer GetSyncBlockObject(uint index)
{
    TargetPointer syncTableEntries = target.ReadGlobalPointer("SyncTableEntries");
    ulong offsetInSyncTable = index * /* SyncTableEntry size */;
    return target.ReadPointer(syncTableEntries + offsetInSyncTable + /* SyncTableEntry::Object offset */);
}

bool IsSyncBlockFree(uint index)
{
    TargetPointer syncTableEntries = target.ReadGlobalPointer("SyncTableEntries");
    ulong offsetInSyncTable = index * /* SyncTableEntry size */;
    TargetPointer obj = target.ReadPointer(syncTableEntries + offsetInSyncTable + /* SyncTableEntry::Object offset */);
    return (obj.Value & 1) != 0;
}

uint GetSyncBlockCount()
{
    TargetPointer syncBlockCache = target.ReadPointer(target.ReadGlobalPointer("SyncBlockCache"));
    uint freeSyncTableIndex = target.Read<uint>(syncBlockCache + /* SyncBlockCache::FreeSyncTableIndex offset */);
    return freeSyncTableIndex - 1;
}

bool TryGetLockInfo(TargetPointer syncBlock, out uint owningThreadId, out uint recursion)
{
    owningThreadId = 0;
    recursion = 0;

    TargetPointer lockObject = target.ReadPointer(syncBlock + /* SyncBlock::Lock offset */);

    if (lockObject != TargetPointer.Null)
    {
        // Resolve the layout of System.Threading.Lock via ManagedTypeSource.
        Target.TypeInfo lockType = target.Contracts.ManagedTypeSource.GetTypeInfo("System.Threading.Lock");
        uint state = target.Read<uint>(lockObject + /* Object data offset */ + (uint)lockType.Fields["_state"].Offset);
        bool monitorHeld = (state & 1) != 0;
        if (monitorHeld)
        {
            owningThreadId = target.Read<uint>(lockObject + /* Object data offset */ + (uint)lockType.Fields["_owningThreadId"].Offset);
            recursion = target.Read<uint>(lockObject + /* Object data offset */ + (uint)lockType.Fields["_recursionCount"].Offset);
        }

        return monitorHeld;
    }

    uint thinLock = target.Read<uint>(syncBlock + /* SyncBlock::ThinLock offset */);
    if (thinLock != 0)
    {
        owningThreadId = thinLock & target.ReadGlobal<uint>("SyncBlockMaskLockThreadId");
        bool monitorHeld = owningThreadId != 0;
        if (monitorHeld)
        {
            recursion = (thinLock & target.ReadGlobal<uint>("SyncBlockMaskLockRecursionLevel"))
                >> (int)target.ReadGlobal<uint>("SyncBlockRecursionLevelShift");
        }

        return monitorHeld;
    }

    return false;
}

uint GetAdditionalThreadCount(TargetPointer syncBlock)
{
    // TODO: read conditional weaktable
    return 0;
}

// Returns the first sync block in the cleanup list, or TargetPointer.Null if the list is empty.
TargetPointer GetSyncBlockFromCleanupList()
{
    TargetPointer syncBlockCache = target.ReadPointer(target.ReadGlobalPointer("SyncBlockCache"));
    TargetPointer cleanupBlockList = target.ReadPointer(syncBlockCache + /* SyncBlockCache::CleanupBlockList offset */);
    if (cleanupBlockList == TargetPointer.Null)
        return TargetPointer.Null;
    return cleanupBlockList;
}

// Returns the next sync block in the cleanup list after syncBlock, or TargetPointer.Null if there is none.
TargetPointer GetNextSyncBlock(TargetPointer syncBlock)
{
    TargetPointer linkNext = target.ReadPointer(syncBlock + /* SyncBlock::LinkNext offset */);
    if (linkNext == TargetPointer.Null)
        return TargetPointer.Null;
    return linkNext;
}

// Gets the built-in COM interop data directly from a sync block.
// This is safe to call even when the associated managed object is not valid (e.g. during cleanup).
bool GetBuiltInComData(TargetPointer syncBlock, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf)
{
    rcw = TargetPointer.Null;
    ccw = TargetPointer.Null;
    ccf = TargetPointer.Null;

    TargetPointer interopInfo = target.ReadPointer(syncBlock + /* SyncBlock::InteropInfo offset */);
    if (interopInfo == TargetPointer.Null)
        return false;

    // RCW: bit 0 is a lock bit used internally; mask it off to get the real pointer.
    TargetPointer rcwRaw = target.ReadPointer(interopInfo + /* InteropSyncBlockInfo::RCW offset */);
    rcw = rcwRaw & ~1ul;

    // CCW and CCF: sentinel value 0x1 means "previously had a CCW/CCF, now null".
    TargetPointer ccwRaw = target.ReadPointer(interopInfo + /* InteropSyncBlockInfo::CCW offset */);
    ccw = (ccwRaw == 1) ? TargetPointer.Null : ccwRaw;

    TargetPointer ccfRaw = target.ReadPointer(interopInfo + /* InteropSyncBlockInfo::CCF offset */);
    ccf = (ccfRaw == 1) ? TargetPointer.Null : ccfRaw;

    return rcw != TargetPointer.Null || ccw != TargetPointer.Null || ccf != TargetPointer.Null;
}
```
