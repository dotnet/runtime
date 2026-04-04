// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class ExceptionFactory : IContractFactory<IException>
{
    IException IContractFactory<IException>.CreateContract(Target target, int version)
    {
        return version switch
        {
            1 => new Exception_1(target),
            _ => default(Exception),
        };
    }
}
