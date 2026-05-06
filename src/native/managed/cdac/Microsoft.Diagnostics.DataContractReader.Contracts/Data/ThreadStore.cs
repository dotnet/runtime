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

        ThreadCount = target.ReadField<int>(address, type, nameof(ThreadCount));
        FirstThreadLink = target.ReadPointerField(address, type, nameof(FirstThreadLink));
        UnstartedCount = target.ReadField<int>(address, type, nameof(UnstartedCount));
        BackgroundCount = target.ReadField<int>(address, type, nameof(BackgroundCount));
        PendingCount = target.ReadField<int>(address, type, nameof(PendingCount));
        DeadCount = target.ReadField<int>(address, type, nameof(DeadCount));
    }

    public int ThreadCount { get; init; }
    public TargetPointer FirstThreadLink { get; init; }
    public int UnstartedCount { get; init; }
    public int BackgroundCount { get; init; }
    public int PendingCount { get; init; }
    public int DeadCount { get; init; }
}
