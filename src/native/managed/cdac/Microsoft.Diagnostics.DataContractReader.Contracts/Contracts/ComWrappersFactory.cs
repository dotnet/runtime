// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class ComWrappersFactory : IContractFactory<IComWrappers>
{
    IComWrappers IContractFactory<IComWrappers>.CreateContract(Target target, int version)
    {
        return version switch
        {
            1 => new ComWrappers_1(target),
            _ => default(ComWrappers),
        };
    }
}
