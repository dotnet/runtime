// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class StackWalkFactory : IContractFactory<IStackWalk>
{
    IStackWalk IContractFactory<IStackWalk>.CreateContract(Target target, int version)
    {
        return version switch
        {
            1 => new StackWalk_1(target),
            _ => default(StackWalk),
        };
    }
}
