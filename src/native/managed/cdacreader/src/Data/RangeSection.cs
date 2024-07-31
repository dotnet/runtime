// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class RangeSection : IData<RangeSection>
{
    static RangeSection IData<RangeSection>.Create(Target target, TargetPointer address)
        => new RangeSection(target, address);

    public RangeSection(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RangeSection);
        RangeBegin = target.ReadPointer(address + (ulong)type.Fields[nameof(RangeBegin)].Offset);
        RangeEndOpen = target.ReadPointer(address + (ulong)type.Fields[nameof(RangeEndOpen)].Offset);
        NextForDelete = target.ReadPointer(address + (ulong)type.Fields[nameof(NextForDelete)].Offset);
    }

    public TargetPointer RangeBegin { get; init; }
    public TargetPointer RangeEndOpen { get; init; }
    public TargetPointer NextForDelete { get; init; }
}
