// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

public sealed class Array : IData<Array>
{
    static Array IData<Array>.Create(Target target, TargetPointer address)
        => new Array(target, address);

    public Array(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Array);

        NumComponents = target.Read<uint>(address + (ulong)type.Fields[FieldNames.NumComponents].Offset);
    }

    public uint NumComponents { get; init; }

    public static class FieldNames
    {
        public const string NumComponents = $"m_{nameof(NumComponents)}";
    }
}
