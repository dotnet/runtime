// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class ExternalMemoryHandleRootData
{
    public bool IsInteriorPointer { get; init; }
    public TargetPointer Address { get; init; }
    public TargetPointer Object { get; init; }
}

public interface IExternalMemoryHandles : IContract
{
    static string IContract.Name => nameof(ExternalMemoryHandles);

    IReadOnlyList<ExternalMemoryHandleRootData> GetRoots(bool resolveInteriorPointers) => throw new NotImplementedException();
}

public readonly struct ExternalMemoryHandles : IExternalMemoryHandles
{
    // Everything throws NotImplementedException
}
