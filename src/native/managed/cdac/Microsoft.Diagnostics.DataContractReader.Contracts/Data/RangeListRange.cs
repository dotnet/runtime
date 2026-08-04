// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.RangeListRange))]
internal sealed partial class RangeListRange : IData<RangeListRange>
{
    [Field] public partial TargetPointer Start { get; }
    [Field] public partial TargetPointer End { get; }
    [Field] public partial TargetPointer Id { get; }
}
