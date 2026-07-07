// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.StressLogChunk))]
internal sealed partial class StressLogChunk : IData<StressLogChunk>
{
    [Field] public TargetPointer Next { get; }

    [FieldAddress]
    public TargetPointer Buf { get; }

    [Field] public uint Sig1 { get; }
    [Field] public uint Sig2 { get; }
}
