// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class CoreLibBinder : IData<CoreLibBinder>
{
    static CoreLibBinder IData<CoreLibBinder>.Create(Target target, TargetPointer address) => new CoreLibBinder(target, address);
    public CoreLibBinder(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.CoreLibBinder);

        Classes = target.ReadPointer(address + (ulong)type.Fields[nameof(Classes)].Offset);
    }
    public TargetPointer Classes { get; init; }
}
