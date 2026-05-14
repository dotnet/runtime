// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class InterpreterPrecodeData : IData<InterpreterPrecodeData>
{
    static InterpreterPrecodeData IData<InterpreterPrecodeData>.Create(Target target, TargetPointer address)
        => new InterpreterPrecodeData(target, address);

    public InterpreterPrecodeData(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.InterpreterPrecodeData);
        ByteCodeAddr = target.ReadPointerField(address, type, nameof(ByteCodeAddr));
        Type = target.ReadField<byte>(address, type, nameof(Type));
    }

    public TargetPointer ByteCodeAddr { get; init; }
    public byte Type { get; init; }
}
