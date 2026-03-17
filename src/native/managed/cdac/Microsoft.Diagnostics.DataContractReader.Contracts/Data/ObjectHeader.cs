// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ObjectHeader : IData<ObjectHeader>
{
    static ObjectHeader IData<ObjectHeader>.Create(Target target, TargetPointer address)
        => new ObjectHeader(target, address);

    public ObjectHeader(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ObjectHeader);
        SyncBlockValue = target.Read<uint>(address + (ulong)type.Fields[nameof(SyncBlockValue)].Offset);
    }

    public uint SyncBlockValue { get; init; }
}
