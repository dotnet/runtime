// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class HandleTableBucket : IData<HandleTableBucket>
{
    static HandleTableBucket IData<HandleTableBucket>.Create(Target target, TargetPointer address) => new HandleTableBucket(target, address);
    public HandleTableBucket(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.HandleTableBucket);

        Table = target.ReadPointer(address + (ulong)type.Fields[nameof(Table)].Offset);
        HandleTableIndex = target.Read<uint>(address + (ulong)type.Fields[nameof(HandleTableIndex)].Offset);
    }

    public TargetPointer Table { get; }
    public uint HandleTableIndex { get; }
}
