// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class EcmaMetadataFactory : IContractFactory<IEcmaMetadata>
{
    IEcmaMetadata IContractFactory<IEcmaMetadata>.CreateContract(Target target, int version)
    {
        return version switch
        {
            1 => new EcmaMetadata_1(target),
            _ => default(EcmaMetadata),
        };
    }
}
