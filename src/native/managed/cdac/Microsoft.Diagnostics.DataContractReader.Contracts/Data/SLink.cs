// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class SLink : IData<SLink>
{
    static SLink IData<SLink>.Create(Target target, TargetPointer address)
        => new SLink(target, address);

    public SLink(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SLink);

        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
    }

    public TargetPointer Next { get; init; }
}
