// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.GCAllocContext))]
internal sealed partial class GCAllocContext : IData<GCAllocContext>
{
    [Field] public partial TargetPointer Pointer { get; }
    [Field] public partial TargetPointer Limit { get; }
    [Field] public partial long AllocBytes { get; }
    [Field] public partial long AllocBytesLoh { get; }
}
