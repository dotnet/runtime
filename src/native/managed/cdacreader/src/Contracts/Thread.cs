// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

// TODO: [cdac] Add other counts / threads
public record struct ThreadStoreData(
    int ThreadCount,
    TargetPointer FirstThread);

internal class Thread : IContract
{
    public static Thread NotImplemented = new Thread();

    public static string Name { get; } = nameof(Thread);

    public static IContract Create(Target target, int version)
    {
        if (!target.TryReadGlobalPointer(Constants.Globals.ThreadStore, out TargetPointer threadStore))
            return NotImplemented;

        return version switch
        {
            1 => new Thread_1(target, threadStore),
            _ => NotImplemented,
        };
    }

    public virtual ThreadStoreData GetThreadStoreData() => throw new NotImplementedException();
}

internal sealed class Thread_1 : Thread
{
    private readonly Target _target;
    private readonly TargetPointer _threadStoreAddr;

    internal Thread_1(Target target, TargetPointer threadStore)
    {
        _target = target;
        _threadStoreAddr = threadStore;
    }

    public override ThreadStoreData GetThreadStoreData()
    {
        Data.ThreadStore? threadStore;
        if (!_target.ProcessedData.TryGet(_threadStoreAddr.Value, out threadStore))
        {
            threadStore = new Data.ThreadStore(_target, _threadStoreAddr);

            // Still okay if processed data is already registered by someone else
            _ = _target.ProcessedData.TryRegister(_threadStoreAddr.Value, threadStore);
        }

        return new ThreadStoreData(threadStore.ThreadCount, threadStore.FirstThread);
    }
}
