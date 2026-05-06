// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class PortableEntryPoint : IData<PortableEntryPoint>
{
    static PortableEntryPoint IData<PortableEntryPoint>.Create(Target target, TargetPointer address) => new PortableEntryPoint(target, address);
    public PortableEntryPoint(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.PortableEntryPoint);

        MethodDesc = target.ReadPointerField(address, type, nameof(MethodDesc));
    }
    public TargetPointer MethodDesc { get; }
}
