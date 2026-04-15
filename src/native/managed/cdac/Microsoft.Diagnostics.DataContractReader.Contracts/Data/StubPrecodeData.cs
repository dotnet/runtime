// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class StubPrecodeData_1 : IData<StubPrecodeData_1>
{
    static StubPrecodeData_1 IData<StubPrecodeData_1>.Create(Target target, TargetPointer address)
        => new StubPrecodeData_1(target, address);

    public StubPrecodeData_1(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.StubPrecodeData);
        MethodDesc = target.ReadPointerField(address, type, nameof(MethodDesc));
        Type = target.ReadField<byte>(address, type, nameof(Type));
    }

    public TargetPointer MethodDesc { get; init; }
    public byte Type { get; init; }
}

internal sealed class StubPrecodeData_2 : IData<StubPrecodeData_2>
{
    static StubPrecodeData_2 IData<StubPrecodeData_2>.Create(Target target, TargetPointer address)
        => new StubPrecodeData_2(target, address);

    public StubPrecodeData_2(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.StubPrecodeData);
        SecretParam = target.ReadPointerField(address, type, nameof(SecretParam));
        Type = target.ReadField<byte>(address, type, nameof(Type));
    }

    public TargetPointer SecretParam { get; init; }
    public byte Type { get; init; }
}
