// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct SyncBlock_1 : ISyncBlock
{
    private const string LockStateName = "_state";
    private const string LockOwningThreadIdName = "_owningThreadId";
    private const string LockRecursionCountName = "_recursionCount";
    private const string LockName = "Lock";
    private const string LockNamespace = "System.Threading";
    private readonly Target _target;
    private readonly TargetPointer _syncTableEntries;
    private readonly ulong _syncBlockLinkOffset;

    internal SyncBlock_1(Target target, TargetPointer syncTableEntries)
    {
        _target = target;
        _syncTableEntries = syncTableEntries;
        _syncBlockLinkOffset = (ulong)target.GetTypeInfo(DataType.SyncBlock).Fields[nameof(Data.SyncBlock.LinkNext)].Offset;
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
            ILoader loader = _target.Contracts.Loader;
            TargetPointer systemAssembly = loader.GetSystemAssembly();
            ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(systemAssembly);

            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            IEcmaMetadata ecmaMetadataContract = _target.Contracts.EcmaMetadata;
            TypeHandle lockType = rts.GetTypeByNameAndModule(LockName, LockNamespace, moduleHandle);
            MetadataReader mdReader = ecmaMetadataContract.GetMetadata(moduleHandle)!;
            TargetPointer lockObjPtr = sb.Lock.Object;
            Data.Object lockObj = _target.ProcessedData.GetOrAdd<Data.Object>(lockObjPtr);
            TargetPointer dataAddr = lockObj.Data;
            uint state = ReadUintField(lockType, LockStateName, rts, mdReader, dataAddr);
            bool monitorHeld = (state & 1) != 0;
            if (monitorHeld)
            {
                owningThreadId = ReadUintField(lockType, LockOwningThreadIdName, rts, mdReader, dataAddr);
                recursion = ReadUintField(lockType, LockRecursionCountName, rts, mdReader, dataAddr);
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
        Data.SyncBlock sb = _target.ProcessedData.GetOrAdd<Data.SyncBlock>(syncBlock);
        uint threadCount = 0;
        TargetPointer next = sb.LinkNext;
        while (next != TargetPointer.Null && threadCount < 1000)
        {
            threadCount++;
            next = _target.ProcessedData.GetOrAdd<Data.SLink>(next).Next;
        }
        return threadCount;
    }

    public TargetPointer GetSyncBlockFromCleanupList()
    {
        TargetPointer syncBlockCache = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.SyncBlockCache));
        Data.SyncBlockCache cache = _target.ProcessedData.GetOrAdd<Data.SyncBlockCache>(syncBlockCache);
        TargetPointer cleanupBlockList = cache.CleanupBlockList;
        if (cleanupBlockList == TargetPointer.Null)
            return TargetPointer.Null;
        return new TargetPointer(cleanupBlockList.Value - _syncBlockLinkOffset);
    }

    public TargetPointer GetNextSyncBlock(TargetPointer syncBlock)
    {
        Data.SyncBlock sb = _target.ProcessedData.GetOrAdd<Data.SyncBlock>(syncBlock);
        if (sb.LinkNext == TargetPointer.Null)
            return TargetPointer.Null;
        return new TargetPointer(sb.LinkNext.Value - _syncBlockLinkOffset);
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

        rcw = interopInfo.RCW & ~1ul;
        ccw = interopInfo.CCW == 1 ? TargetPointer.Null : interopInfo.CCW;
        ccf = interopInfo.CCF == 1 ? TargetPointer.Null : interopInfo.CCF;
        return rcw != TargetPointer.Null || ccw != TargetPointer.Null || ccf != TargetPointer.Null;
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
}
