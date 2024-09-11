// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class RealCodeHeader : IData<RealCodeHeader>
{
    static RealCodeHeader IData<RealCodeHeader>.Create(ITarget target, TargetPointer address)
        => new RealCodeHeader((Target)target, address);

    public RealCodeHeader(Target target, TargetPointer address)
    {
        ITarget.TypeInfo type = target.GetTypeInfo(DataType.RealCodeHeader);
        MethodDesc = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodDesc)].Offset);
    }

    public TargetPointer MethodDesc { get; init; }
}
