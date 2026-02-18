// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class HandleTableMap : IData<HandleTableMap>
{
    static HandleTableMap IData<HandleTableMap>.Create(Target target, TargetPointer address)
        => new HandleTableMap(target, address);

    public HandleTableMap(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.HandleTableMap);
        TargetPointer bucketsPtr = target.ReadPointer(address + (ulong)type.Fields[nameof(BucketsPtr)].Offset);
        uint arrayLength = target.ReadGlobal<uint>(Constants.Globals.InitialHandleTableArraySize);
        for (int i = 0; i < arrayLength; i++)
        {
            TargetPointer bucketPtr = target.ReadPointer(bucketsPtr + (ulong)(i * target.PointerSize));
            BucketsPtr.Add(bucketPtr);
        }
        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
    }

    public List<TargetPointer> BucketsPtr { get; init; } = new List<TargetPointer>();
    public TargetPointer Next { get; init; }
}
