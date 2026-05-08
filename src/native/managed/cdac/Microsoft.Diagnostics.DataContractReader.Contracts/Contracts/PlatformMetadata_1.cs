// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct PlatformMetadata_1 : IPlatformMetadata
{
    internal readonly Target _target;
    private readonly Data.PlatformMetadata _cdacMetadata;

    public PlatformMetadata_1(Target target)
    {
        _target = target;
        TargetPointer cdacMetadataAddress = target.ReadGlobalPointer(Constants.Globals.PlatformMetadata);
        _cdacMetadata = target.ProcessedData.GetOrAdd<Data.PlatformMetadata>(cdacMetadataAddress);
    }

    TargetPointer IPlatformMetadata.GetPrecodeMachineDescriptor()
    {
        return _cdacMetadata.PrecodeMachineDescriptor;
    }

    CodePointerFlags IPlatformMetadata.GetCodePointerFlags()
    {
        return (CodePointerFlags)_cdacMetadata.CodePointerFlags;
    }
}
