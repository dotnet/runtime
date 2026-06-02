// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.RangeSectionFragment))]
internal sealed partial class RangeSectionFragment : IData<RangeSectionFragment>
{
    [Field] public TargetPointer RangeBegin { get; }
    [Field] public TargetPointer RangeEndOpen { get; }
    [Field] public TargetPointer RangeSection { get; }

    /// <summary>
    /// The Next pointer uses the low bit as a collectible flag
    /// (see <c>RangeSectionFragmentPointer</c> in codeman.h).
    /// The OnInit handler strips it to get the actual address.
    /// </summary>
    public TargetPointer Next { get; private set; }

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RangeSectionFragment);
        Next = target.ReadPointerField(address, type, nameof(Next)) & ~1ul;
    }

    public bool Contains(TargetCodePointer address)
        => RangeBegin <= address && address < RangeEndOpen;
}
