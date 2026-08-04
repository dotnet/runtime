// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.RangeListBlock))]
internal sealed partial class RangeListBlock : IData<RangeListBlock>
{
    [FieldAddress]
    public partial TargetPointer Ranges { get; }

    [Field]
    public partial TargetPointer Next { get; }
}
