// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface IReJIT : IContract
{
    static string IContract.Name { get; } = nameof(ReJIT);
    bool IsEnabled() => throw new NotImplementedException();
}

internal readonly struct ReJIT : IReJIT
{

}
