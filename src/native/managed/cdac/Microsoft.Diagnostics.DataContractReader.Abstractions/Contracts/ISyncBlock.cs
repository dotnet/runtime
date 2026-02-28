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
    TargetPointer GetNextSyncBlock(TargetPointer syncBlock) => throw new NotImplementedException();
    /// <summary>
    /// Gets the built-in COM interop data for a given sync block.
    /// </summary>
    /// <param name="syncBlock">Address of the sync block.</param>
    /// <param name="rcw">Receives the RCW pointer (bit 0 masked), or <see cref="TargetPointer.Null"/> if none.</param>
    /// <param name="ccw">Receives the CCW pointer, or <see cref="TargetPointer.Null"/> if none.</param>
    /// <param name="ccf">Receives the CCF pointer, or <see cref="TargetPointer.Null"/> if none.</param>
    /// <returns><see langword="true"/> if any of the COM pointers are non-null; otherwise <see langword="false"/>.</returns>
    bool GetBuiltInComData(TargetPointer syncBlock, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf) => throw new NotImplementedException();
}

public readonly struct SyncBlock : ISyncBlock
{
    // Everything throws NotImplementedException
}
