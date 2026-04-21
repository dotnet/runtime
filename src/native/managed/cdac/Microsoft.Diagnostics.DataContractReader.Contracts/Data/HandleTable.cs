// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class HandleTable : IData<HandleTable>
{
    static HandleTable IData<HandleTable>.Create(Target target, TargetPointer address)
        => new HandleTable(target, address);

    public HandleTable(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.HandleTable);
        SegmentList = target.ReadPointerField(address, type, nameof(SegmentList));
    }

    public TargetPointer SegmentList { get; init; }
}
