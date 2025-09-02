// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public struct SyncBlockData
{

}

public interface ISyncBlock : IContract
{
    static string IContract.Name { get; } = nameof(SyncBlock);

    uint GetSyncBlockCount() => throw new NotImplementedException();
    SyncBlockData GetSyncBlockData(uint index) => throw new NotImplementedException();
}

public readonly struct SyncBlock : ISyncBlock
{
    // Everything throws NotImplementedException
}
