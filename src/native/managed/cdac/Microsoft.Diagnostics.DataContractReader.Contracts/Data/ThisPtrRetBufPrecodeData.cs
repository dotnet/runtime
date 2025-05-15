// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ThisPtrRetBufPrecodeData : IData<ThisPtrRetBufPrecodeData>
{
    static ThisPtrRetBufPrecodeData IData<ThisPtrRetBufPrecodeData>.Create(Target target, TargetPointer address)
        => new ThisPtrRetBufPrecodeData(target, address);

    public ThisPtrRetBufPrecodeData(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ThisPtrRetBufPrecodeData);
        MethodDesc = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodDesc)].Offset);
    }

    public TargetPointer MethodDesc { get; init; }
}
