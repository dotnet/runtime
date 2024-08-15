// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Array : IData<Array>
{
    static Array IData<Array>.Create(Target target, TargetPointer address)
        => new Array(target, address);

    public Array(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Array);

        NumComponents = target.Read<uint>(address + (ulong)type.Fields["m_NumComponents"].Offset);
    }

    public uint NumComponents { get; init; }
}
