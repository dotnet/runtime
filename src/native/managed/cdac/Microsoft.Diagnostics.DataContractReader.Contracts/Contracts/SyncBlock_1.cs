// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct SyncBlock_1 : ISyncBlock
{
    private readonly Target _target;

    internal SyncBlock_1(Target target)
    {
        _target = target;
    }

    public uint GetSyncBlockCount() => throw new NotImplementedException();
    public SyncBlockData GetSyncBlockData(uint index) => throw new NotImplementedException();
}
