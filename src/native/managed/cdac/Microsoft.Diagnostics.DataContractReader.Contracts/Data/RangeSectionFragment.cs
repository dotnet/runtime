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
        RangeBegin = target.ReadPointerField(address, type, nameof(RangeBegin));
        RangeEndOpen = target.ReadPointerField(address, type, nameof(RangeEndOpen));
        RangeSection = target.ReadPointerField(address, type, nameof(RangeSection));
        // The Next pointer uses the low bit as a collectible flag (see RangeSectionFragmentPointer in codeman.h).
        // Strip it to get the actual address.
        Next = target.ReadPointerField(address, type, nameof(Next)) & ~1ul;
    }

    public TargetPointer RangeBegin { get; init; }
    public TargetPointer RangeEndOpen { get; init; }
    public TargetPointer RangeSection { get; init; }
    public TargetPointer Next { get; init; }

    public bool Contains(TargetCodePointer address)
        => RangeBegin <= address && address < RangeEndOpen;
}
