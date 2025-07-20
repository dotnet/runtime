// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class BinderSpaceAssembly : IData<BinderSpaceAssembly>
{
    static BinderSpaceAssembly IData<BinderSpaceAssembly>.Create(Target target, TargetPointer address) => new BinderSpaceAssembly(target, address);
    public BinderSpaceAssembly(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.BinderSpaceAssembly);
        Binder = target.ReadPointer(address + (ulong)type.Fields[nameof(Binder)].Offset);
    }
    public TargetPointer Binder { get; init; }
}
