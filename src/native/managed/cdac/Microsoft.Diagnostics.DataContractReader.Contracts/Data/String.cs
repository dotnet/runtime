// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class String : IData<String>
{
    static String IData<String>.Create(Target target, TargetPointer address)
        => new String(target, address);

    public String(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.String);

        FirstChar = address + (ulong)type.Fields["m_FirstChar"].Offset;
        StringLength = target.Read<uint>(address + (ulong)type.Fields["m_StringLength"].Offset);
    }

    public TargetPointer FirstChar { get; init; }
    public uint StringLength { get; init; }
}
