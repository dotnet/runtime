// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.MethodDescChunk))]
internal sealed partial class MethodDescChunk : IData<MethodDescChunk>
{
    [Field] public partial TargetPointer MethodTable { get; }
    [Field] public partial TargetPointer Next { get; }
    [Field] public partial byte Size { get; }
    [Field] public partial byte Count { get; }
    [Field] public partial ushort FlagsAndTokenRange { get; }

    // The first MethodDesc is at the end of the MethodDescChunk
    [InstanceDataStart]
    public partial TargetPointer FirstMethodDesc { get; }
}
