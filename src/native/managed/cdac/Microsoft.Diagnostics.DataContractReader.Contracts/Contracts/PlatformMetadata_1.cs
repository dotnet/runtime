// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct PlatformMetadata_1 : IPlatformMetadata
{
    internal readonly Target _target;
    private readonly Data.PlatformMetadata _cdacMetadata;

    public PlatformMetadata_1(Target target, Data.PlatformMetadata cdacMetadata)
    {
        _target = target;
        _cdacMetadata = cdacMetadata;
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
