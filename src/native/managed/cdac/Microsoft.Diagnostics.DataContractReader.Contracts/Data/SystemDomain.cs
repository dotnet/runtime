// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class SystemDomain : IData<SystemDomain>
{
    static SystemDomain IData<SystemDomain>.Create(Target target, TargetPointer address) => new SystemDomain(target, address);
    public SystemDomain(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SystemDomain);
        GlobalLoaderAllocator = address + (ulong)type.Fields[nameof(GlobalLoaderAllocator)].Offset;
    }

    public TargetPointer GlobalLoaderAllocator { get; init; }
}
