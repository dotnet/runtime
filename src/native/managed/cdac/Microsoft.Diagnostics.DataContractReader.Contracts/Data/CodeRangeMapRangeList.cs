// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.CodeRangeMapRangeList))]
internal sealed partial class CodeRangeMapRangeList : IData<CodeRangeMapRangeList>
{
    [Field] public int RangeListType { get; }
}
