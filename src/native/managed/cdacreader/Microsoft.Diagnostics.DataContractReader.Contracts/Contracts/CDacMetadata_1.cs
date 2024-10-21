// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct CDacMetadata_1 : ICDacMetadata
{
    internal readonly Target _target;
    private readonly Data.CDacMetadata _cdacMetadata;

    public CDacMetadata_1(Target target, Data.CDacMetadata cdacMetadata)
    {
        _target = target;
        _cdacMetadata = cdacMetadata;
    }

    TargetPointer ICDacMetadata.GetPrecodeMachineDescriptor()
    {
        return _cdacMetadata.PrecodeMachineDescriptor;
    }
}
