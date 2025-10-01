// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class DebugInfoFactory : IContractFactory<IDebugInfo>
{
    IDebugInfo IContractFactory<IDebugInfo>.CreateContract(Target target, int version)
    {
        return version switch
        {
            1 => new DebugInfo_1(target),
            _ => default(DebugInfo),
        };
    }
}
