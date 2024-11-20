// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ILCodeVersionNode : IData<ILCodeVersionNode>
{
    static ILCodeVersionNode IData<ILCodeVersionNode>.Create(Target target, TargetPointer address) => new ILCodeVersionNode(target, address);
    public ILCodeVersionNode(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ILCodeVersionNode);

        VersionId = target.ReadNUInt(address + (ulong)type.Fields[nameof(VersionId)].Offset);
    }

    public TargetNUInt VersionId { get; init; }
}
