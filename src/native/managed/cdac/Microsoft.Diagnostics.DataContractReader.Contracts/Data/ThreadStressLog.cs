// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ThreadStressLog))]
internal sealed partial class ThreadStressLog : IData<ThreadStressLog>
{
    [Field] public partial TargetPointer Next { get; }
    [Field] public partial ulong ThreadId { get; }
    [Field] public partial bool WriteHasWrapped { get; }
    [Field] public partial TargetPointer CurrentPtr { get; }
    [Field] public partial TargetPointer ChunkListHead { get; }
    [Field] public partial TargetPointer ChunkListTail { get; }
    [Field] public partial TargetPointer CurrentWriteChunk { get; }
}
