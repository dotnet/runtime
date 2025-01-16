// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal interface IStackWalk : IContract
{
    static string IContract.Name => nameof(StackWalk);

    public void TestEntry() => throw new NotImplementedException();
}

internal struct StackWalk : IStackWalk
{
    // Everything throws NotImplementedException
}
