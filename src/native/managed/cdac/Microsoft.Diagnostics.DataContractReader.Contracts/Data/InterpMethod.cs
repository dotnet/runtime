// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class InterpMethod : IData<InterpMethod>
{
    static InterpMethod IData<InterpMethod>.Create(Target target, TargetPointer address)
        => new InterpMethod(target, address);

    public InterpMethod(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.InterpMethod);
        MethodDesc = target.ReadPointerField(address, type, nameof(MethodDesc));
    }

    public TargetPointer MethodDesc { get; init; }
}
