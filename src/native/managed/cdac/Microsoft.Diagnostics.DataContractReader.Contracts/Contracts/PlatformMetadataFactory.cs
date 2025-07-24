// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class PlatformMetadataFactory : IContractFactory<IPlatformMetadata>
{
    IPlatformMetadata IContractFactory<IPlatformMetadata>.CreateContract(Target target, int version)
    {
        TargetPointer cdacMetadataAddress = target.ReadGlobalPointer(Constants.Globals.PlatformMetadata);
        Data.PlatformMetadata cdacMetadata = target.ProcessedData.GetOrAdd<Data.PlatformMetadata>(cdacMetadataAddress);
        return version switch
        {
            1 => new PlatformMetadata_1(target, cdacMetadata),
            _ => default(PlatformMetadata),
        };
    }
}
