// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ThreadStore : IData<ThreadStore>
{
    static ThreadStore IData<ThreadStore>.Create(Target target, TargetPointer address)
        => new ThreadStore(target, address);

    public ThreadStore(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ThreadStore);

        ThreadCount = target.Read<int>(address + (ulong)type.Fields[nameof(ThreadCount)].Offset);
        FirstThreadLink = target.ReadPointer(address + (ulong)type.Fields[nameof(FirstThreadLink)].Offset);
        UnstartedCount = target.Read<int>(address + (ulong)type.Fields[nameof(UnstartedCount)].Offset);
        BackgroundCount = target.Read<int>(address + (ulong)type.Fields[nameof(BackgroundCount)].Offset);
        PendingCount = target.Read<int>(address + (ulong)type.Fields[nameof(PendingCount)].Offset);
        DeadCount = target.Read<int>(address + (ulong)type.Fields[nameof(DeadCount)].Offset);
    }

    public int ThreadCount { get; init; }
    public TargetPointer FirstThreadLink { get; init; }
    public int UnstartedCount { get; init; }
    public int BackgroundCount { get; init; }
    public int PendingCount { get; init; }
    public int DeadCount { get; init; }
}
