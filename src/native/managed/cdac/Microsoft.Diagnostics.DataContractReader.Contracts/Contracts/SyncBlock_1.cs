// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct SyncBlock_1 : ISyncBlock
{
    private const uint WAITER_COUNT_SHIFT = 0x6;
    private const uint IS_LOCKED_MASK = 0x1;

    private readonly Target _target;
    private readonly TargetPointer _syncTableEntries;

    internal SyncBlock_1(Target target, TargetPointer syncTableEntries)
    {
        _target = target;
        _syncTableEntries = syncTableEntries;
    }

    public uint GetSyncBlockCount()
    {
        TargetPointer syncBlockCacheAddr = _target.ReadPointer(
            _target.ReadGlobalPointer(Constants.Globals.SyncBlockCache));
        SyncBlockCache syncBlockCache = _target.ProcessedData.GetOrAdd<SyncBlockCache>(syncBlockCacheAddr);

        // Return the count of sync blocks which have ever been used
        return syncBlockCache.FreeSyncTableIndex - 1;
    }

    SyncBlockData ISyncBlock.GetSyncBlockData(uint index)
    {
        Data.SyncTableEntry entry = GetSyncTableEntry(index);

        if (IsSyncBlockFree(index))
            return new SyncBlockData { IsFree = true };

        if (entry.SyncBlock != TargetPointer.Null)
        {
            Data.SyncBlock syncBlock = _target.ProcessedData.GetOrAdd<Data.SyncBlock>(entry.SyncBlock);
            return new SyncBlockData
            {
                IsFree = false,
                Object = entry.Object,
                SyncBlock = entry.SyncBlock,
                RecursionLevel = syncBlock.Monitor.RecursionLevel,
                HoldingThreadId = syncBlock.Monitor.HoldingThreadId,
                MonitorHeldState = (syncBlock.Monitor.LockState & IS_LOCKED_MASK) | ((syncBlock.Monitor.LockState >> (int)WAITER_COUNT_SHIFT) << 1)
            };
        }

        return new SyncBlockData
        {
            IsFree = false,
            Object = entry.Object,
            SyncBlock = entry.SyncBlock,
            RecursionLevel = 0,
            HoldingThreadId = 0,
            MonitorHeldState = 0
        };
    }

    bool ISyncBlock.TryGetBuiltInComData(uint index, out TargetPointer rcw, out TargetPointer ccw)
    {
        rcw = TargetPointer.Null;
        ccw = TargetPointer.Null;

        Data.SyncTableEntry entry = GetSyncTableEntry(index);

        if (entry.SyncBlock == TargetPointer.Null)
            return false;
        Data.SyncBlock syncBlock = _target.ProcessedData.GetOrAdd<Data.SyncBlock>(entry.SyncBlock);

        if (syncBlock.InteropInfo == TargetPointer.Null)
            return false;
        Data.InteropSyncBlockInfo interopInfo = _target.ProcessedData.GetOrAdd<Data.InteropSyncBlockInfo>(syncBlock.InteropInfo);

        rcw = interopInfo.RCW;
        ccw = interopInfo.CCW;
        return rcw != TargetPointer.Null || ccw != TargetPointer.Null;
    }

    private bool IsSyncBlockFree(uint index)
    {
        Data.SyncTableEntry entry = GetSyncTableEntry(index);
        // Lowest bit is set if this entry is free
        return (entry.Object & 0x1) == 0x1;
    }

    private Data.SyncTableEntry GetSyncTableEntry(uint index)
    {
        ulong offsetInSyncTableEntries = index * (ulong)_target.GetTypeInfo(DataType.SyncTableEntry).Size!;
        return _target.ProcessedData.GetOrAdd<Data.SyncTableEntry>(_syncTableEntries + offsetInSyncTableEntries);
    }
}
