// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.HandleTable))]
internal sealed partial class HandleTable : IData<HandleTable>
{
    [Field] public TargetPointer SegmentList { get; }
}
