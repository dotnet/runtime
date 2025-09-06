# Contract Object

This contract is for getting information about SyncBlocks.

## APIs of contract

```csharp
public readonly struct SyncBlockData
{
    public bool IsFree { get; init; }
    public TargetPointer SyncBlock { get; init; }
    public TargetPointer Object { get; init; }
    public uint RecursionLevel { get; init; }
    public uint HoldingThreadId { get; init; }
    public uint MonitorHeldState { get; init; }
}
```

``` csharp
// Get the number of syncblocks that have ever been used in the target runtime.
uint GetSyncBlockCount();

// Get information about a syncblock at a given index.
SyncBlockData GetSyncBlockData(uint index);

// Get the number of threads waiting on the syncblock at a given index (up to the maximumIteration).
uint GetAdditionalThreadCount(uint index, uint maximumIterations = 1000);

// Get built-in COM data for the syncblock if available. Returns false if the syncblock at the given index does not have COM data.
bool TryGetBuiltInComData(uint index, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer cf);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `InteropSyncBlockInfo` | `ClassFactory` | Pointer to the ClassFactory for the object (if it exists) |
| `InteropSyncBlockInfo` | `RCW` | Pointer to the RCW for the object (if it exists) |
| `InteropSyncBlockInfo` | `CCW` | Pointer to the CCW for the object (if it exists) |
| `SyncBlock` | `Monitor` | `AwareLock` storing the `SyncBlock`s locking state |
| `SyncBlock` | `InteropInfo` | Pointer to an optional `InteropSyncBlockInfo` for the sync block |
| `SyncBlock` | `Link` | `SLink` to list of threads waiting on the `SyncBlock` |
| `SyncTableEntry` | `SyncBlock` | `SyncBlock` corresponding to the entry |
| `SyncTableEntry` | `Object` | `Object` corresponding to the entry |
| `SyncBlockCache` | `FreeSyncTableIndex` | Pointer to the first syncblock index which has never been used in the target runtime |
| `AwareLock` | `RecursionLevel` | RecursionLevel of the `AwareLock` |
| `AwareLock` | `HoldingThreadId` | ThreadId currently holding the `AwareLock` |
| `AwareLock` | `LockState` | `uint` flag mask containing lock state information |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `SyncTableEntries` | TargetPointer | The `SyncTableEntry` list |
| `SyncBlockCache` | TargetPointer | The global `SyncBlockCache` |

Constants used:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `WAITER_COUNT_SHIFT` | uint | AwareLock LockState shift to get waiter count | `0x6` |
| `IS_LOCKED_MASK` | uint | AwareLock LockState mask to determine if locked | `0x1` |


Contracts used:
| Contract Name |
| --- |
| _(none)_ |

``` csharp
uint GetSyncBlockCount()
{
    TargetPointer syncBlockCacheAddr = target.ReadPointer(target.ReadGlobalPointer("SyncBlockCache"));
    uint freeSyncTableIndex = target.Read<uint>(syncBlockCacheAddr + /* SyncBlockCache::FreeSyncTableIndex offset */);
    // freeSyncTableIndex points to the first unused SyncBlock
    return freeSyncTableIndex - 1;
}

SyncBlockData GetSyncBlockData(uint index)
{
    TargetPointer syncTableEntry = target.ReadPointer(GetSyncTableEntryAddress(index));

    TargetPointer pObject = target.ReadPointer(syncTableEntry + /* SyncTableEntry::Object offset */);
    TargetPointer pSyncBlock = target.ReadPointer(syncTableEntry + /* SyncTableEntry::SyncBlock offset */);

    // SyncBlock is free if lowest bit of the Object field is set.
    if ((pObject & 0x1) == 0x1)
        return new SyncBlockData { IsFree = true };

    if (pSyncBlock != TargetPointer.Null)
    {
        TargetPointer pMonitor = target.ReadPointer(pSyncBlock + /* SyncBlock::Monitor offset */);
        uint lockState =  target.Read<uint>(pMonitor + /* AwareLock::LockState offset */)
        bool locked = (lockState & IS_LOCKED_MASK) == IS_LOCKED_MASK;
        uint waiterCount = lockState >> WAITER_COUNT_SHIFT;

        // monitorHeldState lsb is 1 if locked, 0 if unlocked
        // the higher bits contain the waiter count shifted up
        uint monitorHeldState = (locked ? 0x1 : 0x0) | (waiterCount << 1);
        return new SyncBlockData
        {
            IsFree = false,
            Object = pObject,
            SyncBlock = pSyncBlock,
            RecursionLevel = target.Read<uint>(pMonitor + /* AwareLock::RecursionLevel offset */),
            HoldingThreadId = target.Read<uint>(pMonitor + /* AwareLock::HoldingThreadId offset */),
            MonitorHeldState = monitorHeldState,
        };
    }

    return new SyncBlockData
    {
        IsFree = false,
        Object = pObject,
        SyncBlock = pSyncBlock,
        RecursionLevel = 0,
        HoldingThreadId = 0,
        MonitorHeldState = 0
    };
}

uint GetAdditionalThreadCount(uint index, uint maximumIterations)
{
    TargetPointer syncTableEntry = target.ReadPointer(GetSyncTableEntryAddress(index));
    TargetPointer syncBlock = target.ReadPointer(syncTableEntry + /* SyncTableEntry::SyncBlock offset */);
    if (syncBlock == TargetPointer.Null)
        return 0;
    
    uint additionalThreadCount = 0;
    TargetPointer pLink = target.ReadPointer(syncBlock + /* SyncBlock::Link offset */);
    while (pLink != TargetPointer.Null && additionalThreadCount < maximumIterations)
    {
        additionalThreadCount += 1;
        pLink = target.ReadPointer(pLink);
    }

    return additionalThreadCount;
}

bool GetBuiltInComData(uint index, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer cf);
{
    TargetPointer syncTableEntry = target.ReadPointer(GetSyncTableEntryAddress(index));
    TargetPointer syncBlock = target.ReadPointer(syncTableEntry + /* SyncTableEntry::SyncBlock offset */);
    if (syncBlock == TargetPointer.Null)
        return false;

    TargetPointer interopInfo = target.ReadPointer(syncBlock + /* SyncBlock::InteropInfo offset */);
    if (interopInfo == TargetPointer.Null)
        return false;

    rcw = target.ReadPointer(interopInfo + /* InteropSyncBlockInfo::RCW offset */);
    ccw = target.ReadPointer(interopInfo + /* InteropSyncBlockInfo::CCW offset */);
    cf = target.ReadPointer(interopInfo + /* InteropSyncBlockInfo::ClassFactory offset */);
    return rcw != TargetPointer.Null || ccw != TargetPointer.Null || cf != TargetPointer.Null;
}
```

Helpers:
```csharp
private TargetPointer GetSyncTableEntryAddress(uint index)
{
    TargetPointer syncTableEntries = target.ReadPointer(target.ReadGlobalPointer("SyncTableEntries"));
    uint syncTableEntrySize = target.GetTypeInfo(DataType.SyncTableEntry).Size;
    return syncTableEntries + (syncTableEntrySize * index)
}
```