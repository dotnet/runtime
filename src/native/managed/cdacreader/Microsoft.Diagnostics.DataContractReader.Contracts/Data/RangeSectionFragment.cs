// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class RangeSectionFragment : IData<RangeSectionFragment>
{
    static RangeSectionFragment IData<RangeSectionFragment>.Create(Target target, TargetPointer address)
        => new RangeSectionFragment(target, address);

    public RangeSectionFragment(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RangeSectionFragment);
        RangeBegin = target.ReadPointer(address + (ulong)type.Fields[nameof(RangeBegin)].Offset);
        RangeEndOpen = target.ReadPointer(address + (ulong)type.Fields[nameof(RangeEndOpen)].Offset);
        RangeSection = target.ReadPointer(address + (ulong)type.Fields[nameof(RangeSection)].Offset);
        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
    }

    public TargetPointer RangeBegin { get; init; }
    public TargetPointer RangeEndOpen { get; init; }
    public TargetPointer RangeSection { get; init; }
    public TargetPointer Next { get; init; }

    public bool Contains(TargetCodePointer address)
        => RangeBegin <= address && address < RangeEndOpen;
}
