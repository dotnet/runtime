// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class AppDomain : IData<AppDomain>
{
    static AppDomain IData<AppDomain>.Create(Target target, TargetPointer address) => new AppDomain(target, address);
    public AppDomain(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.AppDomain);

        CodeVersionManager = target.ReadPointer(address + (ulong)type.Fields[nameof(CodeVersionManager)].Offset);
    }

    public TargetPointer CodeVersionManager { get; init; }
}
