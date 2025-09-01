// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class AssemblyBinder : IData<AssemblyBinder>
{
    static AssemblyBinder IData<AssemblyBinder>.Create(Target target, TargetPointer address) => new AssemblyBinder(target, address);
    public AssemblyBinder(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.AssemblyBinder);
        AssemblyLoadContext = target.ReadPointer(address + (ulong)type.Fields[nameof(AssemblyLoadContext)].Offset);
    }
    public TargetPointer AssemblyLoadContext { get; init; }
}
