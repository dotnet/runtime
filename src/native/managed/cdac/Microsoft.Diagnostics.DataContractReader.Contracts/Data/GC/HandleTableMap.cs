// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class HandleTableMap : IData<HandleTableMap>
{
    static HandleTableMap IData<HandleTableMap>.Create(Target target, TargetPointer address) => new HandleTableMap(target, address);
    public HandleTableMap(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.HandleTableMap);

        Buckets = target.ReadPointer(address + (ulong)type.Fields[nameof(Buckets)].Offset);
        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
        MaxIndex = target.Read<uint>(address + (ulong)type.Fields[nameof(MaxIndex)].Offset);
    }

    public TargetPointer Buckets { get; }
    public TargetPointer Next { get; }
    public uint MaxIndex { get; }
}
