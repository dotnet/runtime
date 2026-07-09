// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.GCAllocContext))]
internal sealed partial class GCAllocContext : IData<GCAllocContext>
{
    [Field] public TargetPointer Pointer { get; }
    [Field] public TargetPointer Limit { get; }
    [Field] public long AllocBytes { get; }
    [Field] public long AllocBytesLoh { get; }
}
