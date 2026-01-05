// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class LoaderFactory : IContractFactory<ILoader>
{
    ILoader IContractFactory<ILoader>.CreateContract(Target target, int version)
    {
        return version switch
        {
            1 => new Loader_1(target),
            _ => default(Loader),
        };
    }
}
