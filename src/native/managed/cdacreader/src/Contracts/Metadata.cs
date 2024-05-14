// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal struct MethodTable
{
}

internal interface IMetadata : IContract
{
    static string IContract.Name => nameof(Metadata);
    static IContract IContract.Create(Target target, int version)
    {
        return version switch
        {
            1 => new Metadata_1(target),
            _ => default(Metadata),
        };
    }

    public virtual DacpMethodTableData GetMethodTableData(TargetPointer targetPointer) => throw new NotImplementedException();
}

internal struct Metadata : IMetadata
{
    // Everything throws NotImplementedException
}


internal struct Metadata_1 : IMetadata
{
    private readonly Target _target;

    internal Metadata_1(Target target)
    {
        _target = target;
    }


    public DacpMethodTableData GetMethodTableData(TargetPointer targetPointer)
    {
        throw new NotImplementedException();
    }
}
