// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class StubPrecodeData : IData<StubPrecodeData>
{
    static StubPrecodeData IData<StubPrecodeData>.Create(Target target, TargetPointer address)
        => new StubPrecodeData(target, address);

    public StubPrecodeData(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.StubPrecodeData);
        MethodDesc = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodDesc)].Offset);
        Type = target.Read<byte>(address + (ulong)type.Fields[nameof(Type)].Offset);
    }

    public TargetPointer MethodDesc { get; init; }
    public byte Type { get; init; }
}
