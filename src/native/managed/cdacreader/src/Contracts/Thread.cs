// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

// TODO: [cdac] Add other counts / threads
internal record struct ThreadStoreData(
    int ThreadCount,
    TargetPointer FirstThread);

internal interface IThread : IContract
{
    static string IContract.Name { get; } = nameof(Thread);
    static IContract IContract.Create(Target target, int version)
    {
        TargetPointer threadStore = target.ReadGlobalPointer(Constants.Globals.ThreadStore);
        return version switch
        {
            1 => new Thread_1(target, threadStore),
            _ => default(Thread),
        };
    }

    public virtual ThreadStoreData GetThreadStoreData() => throw new NotImplementedException();
}

internal readonly struct Thread : IThread
{
    // Everything throws NotImplementedException
}

internal readonly struct Thread_1 : IThread
{
    private readonly Target _target;
    private readonly TargetPointer _threadStoreAddr;

    internal Thread_1(Target target, TargetPointer threadStore)
    {
        _target = target;
        _threadStoreAddr = threadStore;
    }

    ThreadStoreData IThread.GetThreadStoreData()
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
