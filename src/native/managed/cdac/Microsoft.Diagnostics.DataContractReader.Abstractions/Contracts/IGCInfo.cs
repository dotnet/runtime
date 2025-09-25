// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;


namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public struct GCInfoToken
{
    public int Version;
    public TargetPointer Address;
}

public interface IGCInfoHandle { }

public interface IGCInfo : IContract
{
    static string IContract.Name { get; } = nameof(GCInfo);

    IGCInfoHandle DecodeGCInfo(GCInfoToken token) => throw new NotImplementedException();
    uint GetCodeLength(IGCInfoHandle handle) => throw new NotImplementedException();
}

public readonly struct GCInfo : IGCInfo
{
    // Everything throws NotImplementedException
}
