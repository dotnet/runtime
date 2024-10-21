// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class CDacMetadataFactory : IContractFactory<ICDacMetadata>
{
    ICDacMetadata IContractFactory<ICDacMetadata>.CreateContract(Target target, int version)
    {
        TargetPointer cdacMetadataAddress = target.ReadGlobalPointer(Constants.Globals.CDacMetadata);
        Data.CDacMetadata cdacMetadata = target.ProcessedData.GetOrAdd<Data.CDacMetadata>(cdacMetadataAddress);
        return version switch
        {
            1 => new CDacMetadata_1(target, cdacMetadata),
            _ => default(CDacMetadata),
        };
    }
}
