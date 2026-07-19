// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.EEAllocContext))]
internal sealed partial class EEAllocContext : IData<EEAllocContext>
{
    [Field] public GCAllocContext GCAllocationContext { get; }
}
