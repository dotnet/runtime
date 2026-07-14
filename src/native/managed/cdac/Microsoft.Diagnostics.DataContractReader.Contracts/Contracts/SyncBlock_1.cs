// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct SyncBlock_1 : ISyncBlock
{
    private readonly Target _target;
    private readonly TargetPointer _syncTableEntries;

    internal SyncBlock_1(Target target)
    {
        _target = target;
        _syncTableEntries = target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.SyncTableEntries));
    }

    public TargetPointer GetSyncBlock(uint index)
    {
        Data.SyncTableEntry ste = _target.ProcessedData.GetOrAdd<Data.SyncTableEntry>(_syncTableEntries + index * _target.GetTypeInfo(DataType.SyncTableEntry).Size!.Value);
        return ste.SyncBlock?.Address ?? TargetPointer.Null;
    }

    public TargetPointer GetSyncBlockObject(uint index)
    {
        Data.SyncTableEntry ste = _target.ProcessedData.GetOrAdd<Data.SyncTableEntry>(_syncTableEntries + index * _target.GetTypeInfo(DataType.SyncTableEntry).Size!.Value);
        return ste.Object?.Address ?? TargetPointer.Null;
    }

    public bool IsSyncBlockFree(uint index)
    {
        Data.SyncTableEntry ste = _target.ProcessedData.GetOrAdd<Data.SyncTableEntry>(_syncTableEntries + index * _target.GetTypeInfo(DataType.SyncTableEntry).Size!.Value);
        return (ste.Object?.Address & 1) != 0;
    }

    public uint GetSyncBlockCount()
    {
        TargetPointer syncBlockCache = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.SyncBlockCache));
        Data.SyncBlockCache cache = _target.ProcessedData.GetOrAdd<Data.SyncBlockCache>(syncBlockCache);
        return cache.FreeSyncTableIndex - 1;
    }

    public bool TryGetLockInfo(TargetPointer syncBlock, out uint owningThreadId, out uint recursion)
    {
        owningThreadId = 0;
        recursion = 0;
        Data.SyncBlock sb = _target.ProcessedData.GetOrAdd<Data.SyncBlock>(syncBlock);

        if (sb.Lock != null)
        {
            Data.Lock lockData = _target.ProcessedData.GetOrAdd<Data.Lock>(sb.Lock.Object);
            bool monitorHeld = (lockData.State & 1) != 0;
            if (monitorHeld)
            {
                owningThreadId = (uint)lockData.OwningThreadId;
                recursion = lockData.RecursionCount;
            }
            return monitorHeld;
        }

        else if (sb.ThinLock != 0)
        {
            owningThreadId = sb.ThinLock & _target.ReadGlobal<uint>(Constants.Globals.SyncBlockMaskLockThreadId);
            bool monitorHeld = owningThreadId != 0;
            if (monitorHeld)
            {
                recursion = (sb.ThinLock & _target.ReadGlobal<uint>(Constants.Globals.SyncBlockMaskLockRecursionLevel)) >> (int)_target.ReadGlobal<uint>(Constants.Globals.SyncBlockRecursionLevelShift);
            }
            return monitorHeld;
        }

        else
        {
            return false;
        }
    }

    public uint GetAdditionalThreadCount(TargetPointer syncBlock)
    {
        // TODO: read conditional weak table to get additional thread count
        return 0;
    }

    public TargetPointer GetSyncBlockFromCleanupList()
    {
        TargetPointer syncBlockCache = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.SyncBlockCache));
        Data.SyncBlockCache cache = _target.ProcessedData.GetOrAdd<Data.SyncBlockCache>(syncBlockCache);
        TargetPointer cleanupBlockList = cache.CleanupBlockList;
        if (cleanupBlockList == TargetPointer.Null)
            return TargetPointer.Null;
        return cleanupBlockList;
    }

    public TargetPointer GetNextSyncBlock(TargetPointer syncBlock)
    {
        Data.SyncBlock sb = _target.ProcessedData.GetOrAdd<Data.SyncBlock>(syncBlock);
        if (sb.LinkNext == TargetPointer.Null)
            return TargetPointer.Null;
        return sb.LinkNext;
    }

    public bool GetBuiltInComData(TargetPointer syncBlock, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf)
    {
        rcw = TargetPointer.Null;
        ccw = TargetPointer.Null;
        ccf = TargetPointer.Null;

        Data.SyncBlock sb = _target.ProcessedData.GetOrAdd<Data.SyncBlock>(syncBlock);
        Data.InteropSyncBlockInfo? interopInfo = sb.InteropInfo;
        if (interopInfo == null)
            return false;

        rcw = (interopInfo.RCW ?? TargetPointer.Null) & ~1ul;
        ccw = interopInfo.CCW == 1 ? TargetPointer.Null : (interopInfo.CCW ?? TargetPointer.Null);
        ccf = interopInfo.CCF == 1 ? TargetPointer.Null : (interopInfo.CCF ?? TargetPointer.Null);
        return rcw != TargetPointer.Null || ccw != TargetPointer.Null || ccf != TargetPointer.Null;
    }
}
