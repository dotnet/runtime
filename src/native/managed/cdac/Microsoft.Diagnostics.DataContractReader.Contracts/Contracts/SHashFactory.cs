// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class SHashFactory : IContractFactory<ISHash>
{
    ISHash IContractFactory<ISHash>.CreateContract(Target target, int version)
    {
        return version switch
        {
            1 => new SHash_1(target),
            _ => default(SHash),
        };
    }
}
