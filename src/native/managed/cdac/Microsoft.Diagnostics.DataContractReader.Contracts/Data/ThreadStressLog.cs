// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ThreadStressLog : IData<ThreadStressLog>
{
    static ThreadStressLog IData<ThreadStressLog>.Create(Target target, TargetPointer address)
        => new ThreadStressLog(target, address);

    public ThreadStressLog(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ThreadStressLog);

        Next = target.ReadPointerField(address, type, nameof(Next));
        ThreadId = target.ReadField<ulong>(address, type, nameof(ThreadId));
        WriteHasWrapped = target.ReadField<byte>(address, type, nameof(WriteHasWrapped)) != 0;
        CurrentPtr = target.ReadPointerField(address, type, nameof(CurrentPtr));
        ChunkListHead = target.ReadPointerField(address, type, nameof(ChunkListHead));
        ChunkListTail = target.ReadPointerField(address, type, nameof(ChunkListTail));
        CurrentWriteChunk = target.ReadPointerField(address, type, nameof(CurrentWriteChunk));
    }

    public TargetPointer Next { get; init; }
    public ulong ThreadId { get; init; }
    public bool WriteHasWrapped { get; init; }
    public TargetPointer CurrentPtr { get; init; }
    public TargetPointer ChunkListHead { get; init; }
    public TargetPointer ChunkListTail { get; init; }
    public TargetPointer CurrentWriteChunk { get; init; }
}
