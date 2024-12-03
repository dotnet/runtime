// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface IGCCover : IContract
{
    static string IContract.Name { get; } = nameof(GCCover);

    public virtual TargetPointer? GetGCCoverageInfo(NativeCodeVersionHandle codeVersionHandle) => throw new NotImplementedException();
}

internal readonly struct GCCover : IGCCover
{
    // throws NotImplementedException for all methods
}
