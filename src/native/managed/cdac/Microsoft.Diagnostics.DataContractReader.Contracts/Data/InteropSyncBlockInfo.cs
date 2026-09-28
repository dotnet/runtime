// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.InteropSyncBlockInfo))]
internal sealed partial class InteropSyncBlockInfo : IData<InteropSyncBlockInfo>
{
    [Field] public partial TargetPointer? RCW { get; }
    [Field] public partial TargetPointer? CCW { get; }
    [Field] public partial TargetPointer? CCF { get; }
}
