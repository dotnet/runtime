// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.StressLogChunk))]
internal sealed partial class StressLogChunk : IData<StressLogChunk>
{
    [Field] public partial TargetPointer Next { get; }

    [FieldAddress]
    public partial TargetPointer Buf { get; }

    [Field] public partial uint Sig1 { get; }
    [Field] public partial uint Sig2 { get; }
}
