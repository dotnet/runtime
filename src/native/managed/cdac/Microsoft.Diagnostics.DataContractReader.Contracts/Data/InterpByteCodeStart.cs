// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class InterpByteCodeStart : IData<InterpByteCodeStart>
{
    static InterpByteCodeStart IData<InterpByteCodeStart>.Create(Target target, TargetPointer address)
        => new InterpByteCodeStart(target, address);

    public InterpByteCodeStart(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.InterpByteCodeStart);
        Method = target.ReadPointerField(address, type, nameof(Method));
    }

    public TargetPointer Method { get; init; }
}
