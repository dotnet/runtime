// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public enum GCHeapType
{
    Unknown,
    Workstation,
    Server,
}

public interface IGC : IContract
{
    static string IContract.Name { get; } = nameof(GC);

    GCHeapType GetGCHeapType() => throw new NotImplementedException();
    uint GetGCHeapCount() => throw new NotImplementedException();
    bool GetGCStructuresValid() => throw new NotImplementedException();
    uint GetMaxGeneration() => throw new NotImplementedException();
}

public readonly struct GC : IGC
{
    // Everything throws NotImplementedException
}
