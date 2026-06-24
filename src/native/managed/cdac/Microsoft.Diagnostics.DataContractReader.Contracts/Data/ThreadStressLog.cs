// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ThreadStressLog))]
internal sealed partial class ThreadStressLog : IData<ThreadStressLog>
{
    [Field] public TargetPointer Next { get; }
    [Field] public ulong ThreadId { get; }
    [Field] public bool WriteHasWrapped { get; }
    [Field] public TargetPointer CurrentPtr { get; }
    [Field] public TargetPointer ChunkListHead { get; }
    [Field] public TargetPointer ChunkListTail { get; }
    [Field] public TargetPointer CurrentWriteChunk { get; }
}
