// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;
public interface ISyncBlock : IContract
{
    static string IContract.Name { get; } = nameof(SyncBlock);
    TargetPointer GetSyncBlock(uint index) => throw new NotImplementedException();
    TargetPointer GetSyncBlockObject(uint index) => throw new NotImplementedException();
    bool IsSyncBlockFree(uint index) => throw new NotImplementedException();
    uint GetSyncBlockCount() => throw new NotImplementedException();
    bool TryGetLockInfo(TargetPointer syncBlock, out uint owningThreadId, out uint recursion) => throw new NotImplementedException();
    uint GetAdditionalThreadCount(TargetPointer syncBlock) => throw new NotImplementedException();
    TargetPointer GetSyncBlockFromCleanupList() => throw new NotImplementedException();
    SyncBlockCleanupInfo GetSyncBlockCleanupInfo(TargetPointer syncBlock) => throw new NotImplementedException();
}

public readonly struct SyncBlockCleanupInfo
{
    public SyncBlockCleanupInfo(TargetPointer nextSyncBlock, TargetPointer blockRCW, TargetPointer blockClassFactory, TargetPointer blockCCW)
    {
        NextSyncBlock = nextSyncBlock;
        BlockRCW = blockRCW;
        BlockClassFactory = blockClassFactory;
        BlockCCW = blockCCW;
    }

    public TargetPointer NextSyncBlock { get; }
    public TargetPointer BlockRCW { get; }
    public TargetPointer BlockClassFactory { get; }
    public TargetPointer BlockCCW { get; }
}

public readonly struct SyncBlock : ISyncBlock
{
    // Everything throws NotImplementedException
}
