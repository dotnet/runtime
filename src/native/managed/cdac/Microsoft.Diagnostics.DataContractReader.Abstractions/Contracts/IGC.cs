// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public class GCIdentifiers
{
    public const string Server = "server";
    public const string Workstation = "workstation";

    public const string Regions = "regions";
    public const string Segments = "segments";
}

public interface IGC : IContract
{
    static string IContract.Name { get; } = nameof(GC);

    string[] GetGCIdentifiers() => throw new NotImplementedException();
    uint GetGCHeapCount() => throw new NotImplementedException();
    bool GetGCStructuresValid() => throw new NotImplementedException();
    uint GetMaxGeneration() => throw new NotImplementedException();
    IEnumerable<TargetPointer> GetGCHeaps() => throw new NotImplementedException();
}

public readonly struct GC : IGC
{
    // Everything throws NotImplementedException
}
