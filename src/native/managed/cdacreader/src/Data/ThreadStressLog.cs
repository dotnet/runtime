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

        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
        ThreadId = target.Read<uint>(address + (ulong)type.Fields[nameof(ThreadId)].Offset);
        WriteHasWrapped = target.Read<byte>(address + (ulong)type.Fields[nameof(WriteHasWrapped)].Offset) != 0;
        CurrentPtr = target.ReadPointer(address + (ulong)type.Fields[nameof(CurrentPtr)].Offset);
        ChunkListHead = target.ReadPointer(address + (ulong)type.Fields[nameof(ChunkListHead)].Offset);
        ChunkListTail = target.ReadPointer(address + (ulong)type.Fields[nameof(ChunkListTail)].Offset);
        CurrentWriteChunk = target.ReadPointer(address + (ulong)type.Fields[nameof(CurrentWriteChunk)].Offset);
    }

    public TargetPointer Next { get; init; }
    public ulong ThreadId { get; init; }
    public bool WriteHasWrapped { get; init; }
    public TargetPointer CurrentPtr { get; init; }
    public TargetPointer ChunkListHead { get; init; }
    public TargetPointer ChunkListTail { get; init; }
    public TargetPointer CurrentWriteChunk { get; init; }
}
