// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class CoreLibClassDescription : IData<CoreLibClassDescription>
{
    static CoreLibClassDescription IData<CoreLibClassDescription>.Create(Target target, TargetPointer address) => new CoreLibClassDescription(target, address);
    public CoreLibClassDescription(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.CoreLibClassDescription);

        Name = target.ReadUtf8String(target.ReadPointer(address + (ulong)type.Fields[nameof(Name)].Offset));
        NameSpace = target.ReadUtf8String(target.ReadPointer(address + (ulong)type.Fields[nameof(NameSpace)].Offset));
    }

    public string Name { get; init; }
    public string NameSpace { get; init; }
}
