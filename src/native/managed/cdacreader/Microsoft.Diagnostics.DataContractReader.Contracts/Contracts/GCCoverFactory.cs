// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class GCCoverFactory : IContractFactory<IGCCover>
{
    IGCCover IContractFactory<IGCCover>.CreateContract(Target target, int version)
    {
        return version switch
        {
            1 => new GCCover_1(target),
            _ => default(GCCover),
        };
    }
}
