// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ThreadStore
{
    public ThreadStore(Target target, TargetPointer pointer)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ThreadStore);
        TargetPointer addr = target.ReadPointer(pointer.Value);

        ThreadCount = target.Read<int>(addr.Value + (ulong)type.Fields[nameof(ThreadCount)].Offset);
        FirstThread = TargetPointer.Null;
    }

    public int ThreadCount { get; init; }

    public TargetPointer FirstThread { get; init; }
}
