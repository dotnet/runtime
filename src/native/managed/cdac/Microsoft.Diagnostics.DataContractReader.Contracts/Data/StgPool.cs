// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.StgPool))]
internal sealed partial class StgPool : IData<StgPool>
{
    [Field] public TargetPointer SegData { get; }
    [Field] public TargetPointer NextSegment { get; }
    [Field] public uint DataSize { get; }
}
