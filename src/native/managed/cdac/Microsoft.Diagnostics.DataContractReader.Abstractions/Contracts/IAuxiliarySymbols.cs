// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public interface IAuxiliarySymbols : IContract
{
    static string IContract.Name { get; } = nameof(AuxiliarySymbols);
    bool TryGetAuxiliarySymbolName(TargetPointer ip, [NotNullWhen(true)] out string? symbolName) => throw new NotImplementedException();
}

public readonly struct AuxiliarySymbols : IAuxiliarySymbols
{
    // Everything throws NotImplementedException
}
