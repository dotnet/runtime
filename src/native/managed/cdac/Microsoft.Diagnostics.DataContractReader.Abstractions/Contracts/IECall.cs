// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IECall : IContract
{
    static string IContract.Name { get; } = nameof(ECall);

    TargetPointer MapTargetBackToMethodDesc(TargetCodePointer codePointer) => throw new NotImplementedException();
}

public readonly struct ECall : IECall
{
    // throws NotImplementedException for all methods
}
