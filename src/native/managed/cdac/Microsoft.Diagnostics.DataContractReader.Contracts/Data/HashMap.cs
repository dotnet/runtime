// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class HashMap : IData<HashMap>
{
    static HashMap IData<HashMap>.Create(Target target, TargetPointer address)
        => new HashMap(target, address);

    public HashMap(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.HashMap);

        Buckets = target.ReadPointer(address + (ulong)type.Fields[nameof(Buckets)].Offset);
    }

    public TargetPointer Buckets { get; }
}
