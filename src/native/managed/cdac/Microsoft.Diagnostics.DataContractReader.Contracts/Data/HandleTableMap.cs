// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.HandleTableMap))]
internal sealed partial class HandleTableMap : IData<HandleTableMap>
{
    [Field] public TargetPointer Next { get; }
    public List<TargetPointer> BucketsPtr { get; } = [];

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.HandleTableMap);
        TargetPointer bucketsPtr = target.ReadPointerField(address, type, nameof(BucketsPtr));
        uint arrayLength = target.ReadGlobal<uint>(Constants.Globals.InitialHandleTableArraySize);
        for (int i = 0; i < arrayLength; i++)
        {
            TargetPointer bucketPtr = target.ReadPointer(bucketsPtr + (ulong)(i * target.PointerSize));
            BucketsPtr.Add(bucketPtr);
        }
    }
}
