// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class FixupPrecodeData : IData<FixupPrecodeData>
{
    static FixupPrecodeData IData<FixupPrecodeData>.Create(Target target, TargetPointer address)
        => new FixupPrecodeData(target, address);

    public FixupPrecodeData(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.FixupPrecodeData);
        MethodDesc = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodDesc)].Offset);
    }

    public TargetPointer MethodDesc { get; init; }
}
