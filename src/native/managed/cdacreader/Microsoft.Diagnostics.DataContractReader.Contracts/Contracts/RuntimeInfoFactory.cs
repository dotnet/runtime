// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class RuntimeInfoFactory : IContractFactory<IRuntimeInfo>
{
    IRuntimeInfo IContractFactory<IRuntimeInfo>.CreateContract(Target target, int version)
    {
        return version switch
        {
            1 => new RuntimeInfo_1(target),
            _ => default(RuntimeInfo),
        };
    }
}
