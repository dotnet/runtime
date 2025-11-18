// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IComWrappers : IContract
{
    static string IContract.Name { get; } = nameof(ComWrappers);
    TargetPointer GetComWrappersIdentity(TargetPointer address) => throw new NotImplementedException();
}

public readonly struct ComWrappers : IComWrappers
{
    // Everything throws NotImplementedException
}
