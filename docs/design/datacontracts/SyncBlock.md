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
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `SyncTableEntry` | `SyncBlock` | Pointer to the sync block for a sync table entry |
| `SyncTableEntry` | `Object` | Pointer to the object associated with a sync table entry |
| `SyncBlockCache` | `FreeSyncTableIndex` | One past the highest sync table entry index allocated |
| `SyncBlock` | `Lock` | Optional pointer to a `System.Threading.Lock` object payload |
| `SyncBlock` | `ThinLock` | Thin-lock state bits |
| `SyncBlock` | `LinkNext` | Head pointer for additional waiting threads list |
| `SLink` | `Next` | Next link for the additional waiting threads list |

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
```
