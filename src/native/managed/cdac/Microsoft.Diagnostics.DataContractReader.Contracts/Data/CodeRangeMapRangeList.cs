// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class CodeRangeMapRangeList : IData<CodeRangeMapRangeList>
{
    static CodeRangeMapRangeList IData<CodeRangeMapRangeList>.Create(Target target, TargetPointer address)
        => new CodeRangeMapRangeList(target, address);

    public CodeRangeMapRangeList(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.CodeRangeMapRangeList);
        RangeListType = target.ReadField<int>(address, type, nameof(RangeListType));
    }

    public int RangeListType { get; init; }
}
