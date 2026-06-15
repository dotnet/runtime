// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.MethodDescChunk))]
internal sealed partial class MethodDescChunk : IData<MethodDescChunk>
{
    [Field] public TargetPointer MethodTable { get; }
    [Field] public TargetPointer Next { get; }
    [Field] public byte Size { get; }
    [Field] public byte Count { get; }
    [Field] public ushort FlagsAndTokenRange { get; }

    // The first MethodDesc is at the end of the MethodDescChunk
    [InstanceDataStart]
    public TargetPointer FirstMethodDesc { get; }
}
