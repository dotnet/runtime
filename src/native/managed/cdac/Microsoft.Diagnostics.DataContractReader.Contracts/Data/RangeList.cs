// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.RangeList))]
internal sealed partial class RangeList : IData<RangeList>
{
    [FieldAddress]
    public partial TargetPointer StarterBlock { get; }

    public IReadOnlyList<RangeListBlock> Blocks { get; private set; } = [];
    public IReadOnlyList<RangeListRange> Ranges { get; private set; } = [];

    [MemberNotNull(nameof(Blocks), nameof(Ranges))]
    partial void OnInit(Target target, TargetPointer address)
    {
        List<RangeListBlock> blocks = [];
        List<RangeListRange> ranges = [];
        uint rangeCount = target.ReadGlobal<uint>(Constants.Globals.RangeListRangeCount);
        uint rangeSize = target.GetTypeInfo(DataType.RangeListRange).Size!.Value;
        TargetPointer blockAddress = StarterBlock;

        while (blockAddress != TargetPointer.Null)
        {
            RangeListBlock block = target.ProcessedData.GetOrAdd<RangeListBlock>(blockAddress);
            blocks.Add(block);

            for (uint i = 0; i < rangeCount; i++)
            {
                TargetPointer rangeAddress = block.Ranges + (i * rangeSize);
                ranges.Add(target.ProcessedData.GetOrAdd<RangeListRange>(rangeAddress));
            }

            blockAddress = block.Next;
        }

        Blocks = blocks;
        Ranges = ranges;
    }
}
