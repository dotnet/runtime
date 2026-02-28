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

// Get built-in COM interop data directly from a sync block. Returns false if the sync block has no
// interop info or all COM pointers are null.
bool GetBuiltInComData(TargetPointer syncBlock, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `SyncTableEntry` | `SyncBlock` | Pointer to the sync block for a sync table entry |
| `SyncTableEntry` | `Object` | Pointer to the object associated with a sync table entry |
| `SyncBlockCache` | `FreeSyncTableIndex` | One past the highest sync table entry index allocated |
| `SyncBlockCache` | `CleanupBlockList` | Head of the `SLink` cleanup list (points into `SyncBlock.m_Link`) |
| `SyncBlock` | `Lock` | Optional pointer to a `System.Threading.Lock` object payload |
| `SyncBlock` | `ThinLock` | Thin-lock state bits |
| `SyncBlock` | `LinkNext` | Head pointer for additional waiting threads list / cleanup list link |
| `SyncBlock` | `InteropInfo` | Optional pointer to an `InteropSyncBlockInfo` for the sync block |
| `SLink` | `Next` | Next link for the additional waiting threads list |
| `InteropSyncBlockInfo` | `RCW` | RCW pointer; bit 0 is a lock bit and must be masked off |
| `InteropSyncBlockInfo` | `CCW` | CCW pointer; sentinel value `0x1` means previously had a CCW (treat as null) |
| `InteropSyncBlockInfo` | `CCF` | COM class factory pointer; sentinel value `0x1` means previously had a CCF (treat as null) |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `SyncTableEntries` | TargetPointer | Pointer to the sync table entries array |
| `SyncBlockCache` | TargetPointer | Pointer to the runtime sync block cache |
| `SyncBlockMaskLockThreadId` | uint32 | Mask for extracting thread id from `SyncBlock.ThinLock` |
| `SyncBlockMaskLockRecursionLevel` | uint32 | Mask for extracting recursion level from `SyncBlock.ThinLock` |
| `SyncBlockRecursionLevelShift` | uint32 | Shift value for `SyncBlock.ThinLock` recursion level |

### Contract Constants:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `LockStateName` | string | Field name in `System.Threading.Lock` storing monitor-held state bits. | `_state` |
| `LockOwningThreadIdName` | string | Field name in `System.Threading.Lock` storing owning thread id. | `_owningThreadId` |
| `LockRecursionCountName` | string | Field name in `System.Threading.Lock` storing monitor recursion count. | `_recursionCount` |
| `LockName` | string | Type name used to resolve `System.Threading.Lock`. | `Lock` |
| `LockNamespace` | string | Namespace used to resolve `System.Threading.Lock`. | `System.Threading` |

Contracts used:
| Contract Name |
| --- |
| `Loader` |
| `RuntimeTypeSystem` |
| `EcmaMetadata` |

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
        // Resolve System.Threading.Lock in System.Private.CoreLib by name using RuntimeTypeSystem contract, LockName and LockNamespace.
        uint state = ReadUintField(/* Lock type */, "LockStateName", /* RuntimeTypeSystem contract */, /* MetadataReader for SPC */, lockObject);
        bool monitorHeld = (state & 1) != 0;
        if (monitorHeld)
        {
            owningThreadId = ReadUintField(/* Lock type */, "LockOwningThreadIdName", /* contracts */, lockObject);
            recursion = ReadUintField(/* Lock type */, "LockRecursionCountName", /* contracts */, lockObject);
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

private uint ReadUintField(TypeHandle enclosingType, string fieldName, IRuntimeTypeSystem rts, MetadataReader mdReader, TargetPointer dataAddr)
{
    TargetPointer field = rts.GetFieldDescByName(enclosingType, fieldName);
    uint token = rts.GetFieldDescMemberDef(field);
    FieldDefinitionHandle fieldHandle = (FieldDefinitionHandle)MetadataTokens.Handle((int)token);
    FieldDefinition fieldDef = mdReader.GetFieldDefinition(fieldHandle);
    uint offset = rts.GetFieldDescOffset(field, fieldDef);
    return _target.Read<uint>(dataAddr + offset);
}

uint GetAdditionalThreadCount(TargetPointer syncBlock)
{
    uint threadCount = 0;
    TargetPointer next = target.ReadPointer(syncBlock + /* SyncBlock::LinkNext offset */);
    while (next != TargetPointer.Null && threadCount < 1000)
    {
        threadCount++;
        next = target.ReadPointer(next + /* SLink::Next offset */);
    }

    return threadCount;
}

// Returns the first sync block in the cleanup list, or TargetPointer.Null if the list is empty.
// SyncBlockCache.CleanupBlockList points to SyncBlock.m_Link (i.e. SLink.m_pNext inside the SyncBlock).
// Subtract the LinkNext field offset to recover the SyncBlock base address.
TargetPointer GetSyncBlockFromCleanupList()
{
    TargetPointer syncBlockCache = target.ReadPointer(target.ReadGlobalPointer("SyncBlockCache"));
    TargetPointer cleanupBlockList = target.ReadPointer(syncBlockCache + /* SyncBlockCache::CleanupBlockList offset */);
    if (cleanupBlockList == TargetPointer.Null)
        return TargetPointer.Null;
    return cleanupBlockList - /* SyncBlock::LinkNext offset */;
}

// Returns the next sync block in the cleanup list after syncBlock, or TargetPointer.Null if there is none.
TargetPointer GetNextSyncBlock(TargetPointer syncBlock)
{
    TargetPointer linkNext = target.ReadPointer(syncBlock + /* SyncBlock::LinkNext offset */);
    if (linkNext == TargetPointer.Null)
        return TargetPointer.Null;
    return linkNext - /* SyncBlock::LinkNext offset */;
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
