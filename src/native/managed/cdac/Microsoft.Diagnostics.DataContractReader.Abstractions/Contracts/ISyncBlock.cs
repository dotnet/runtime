// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public class SyncBlockData
{
    public bool IsFree { get; init; }
    public TargetPointer SyncBlock { get; init; }
    public TargetPointer Object { get; init; }
    public uint RecursionLevel { get; init; }
    public uint HoldingThreadId { get; init; }
    public uint MonitorHeldState { get; init; }
}

public interface ISyncBlock : IContract
{
    static string IContract.Name { get; } = nameof(SyncBlock);

    uint GetSyncBlockCount() => throw new NotImplementedException();
    SyncBlockData GetSyncBlockData(uint index) => throw new NotImplementedException();
    uint GetAdditionalThreadCount(uint index, uint maximumIterations = 1000) => throw new NotImplementedException();
    bool TryGetBuiltInComData(uint index, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer cf) => throw new NotImplementedException();
}

public readonly struct SyncBlock : ISyncBlock
{
    // Everything throws NotImplementedException
}
