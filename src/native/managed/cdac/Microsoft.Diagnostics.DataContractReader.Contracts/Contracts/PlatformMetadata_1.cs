// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct PlatformMetadata_1 : IPlatformMetadata
{
    internal readonly Target _target;
    private readonly TargetPointer _cdacMetadataAddress;
    private Data.PlatformMetadata _cdacMetadata
        => _target.ProcessedData.GetOrAdd<Data.PlatformMetadata>(_cdacMetadataAddress);

    public PlatformMetadata_1(Target target)
    {
        _target = target;
        _cdacMetadataAddress = target.ReadGlobalPointer(Constants.Globals.PlatformMetadata);
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
