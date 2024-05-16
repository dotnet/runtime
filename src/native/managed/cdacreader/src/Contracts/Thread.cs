// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal record struct ThreadStoreData(
    int ThreadCount,
    TargetPointer FirstThread,
    TargetPointer FinalizerThread,
    TargetPointer GCThread);

internal record struct ThreadStoreCounts(
    int UnstartedThreadCount,
    int BackgroundThreadCount,
    int PendingThreadCount,
    int DeadThreadCount);

internal interface IThread : IContract
{
    static string IContract.Name { get; } = nameof(Thread);
    static IContract IContract.Create(Target target, int version)
    {
        TargetPointer threadStorePointer = target.ReadGlobalPointer(Constants.Globals.ThreadStore);
        TargetPointer threadStore = target.ReadPointer(threadStorePointer);
        return version switch
        {
            1 => new Thread_1(target, threadStore),
            _ => default(Thread),
        };
    }

    public virtual ThreadStoreData GetThreadStoreData() => throw new NotImplementedException();
    public virtual ThreadStoreCounts GetThreadCounts() => throw new NotImplementedException();
}

internal readonly struct Thread : IThread
{
    // Everything throws NotImplementedException
}

internal readonly struct Thread_1 : IThread
{
    private readonly Target _target;
    private readonly TargetPointer _threadStoreAddr;
    private readonly ulong _threadLinkOffset;

    internal Thread_1(Target target, TargetPointer threadStore)
    {
        _target = target;
        _threadStoreAddr = threadStore;

        Target.TypeInfo type = _target.GetTypeInfo(DataType.Thread);
        _threadLinkOffset = (ulong)type.Fields["LinkNext"].Offset;
    }

    ThreadStoreData IThread.GetThreadStoreData()
    {
        Data.ThreadStore? threadStore;
        if (!_target.ProcessedData.TryGet(_threadStoreAddr, out threadStore))
        {
            threadStore = new Data.ThreadStore(_target, _threadStoreAddr);

            // Still okay if processed data is already registered by someone else
            _ = _target.ProcessedData.TryRegister(_threadStoreAddr, threadStore);
        }

        return new ThreadStoreData(
            threadStore.ThreadCount,
            new TargetPointer(threadStore.FirstThreadLink - _threadLinkOffset),
            _target.ReadGlobalPointer(Constants.Globals.FinalizerThread),
            _target.ReadGlobalPointer(Constants.Globals.GCThread));
    }

    ThreadStoreCounts IThread.GetThreadCounts()
    {
        Data.ThreadStore? threadStore;
        if (!_target.ProcessedData.TryGet(_threadStoreAddr, out threadStore))
        {
            threadStore = new Data.ThreadStore(_target, _threadStoreAddr);

            // Still okay if processed data is already registered by someone else
            _ = _target.ProcessedData.TryRegister(_threadStoreAddr, threadStore);
        }

        return new ThreadStoreCounts(
            threadStore.UnstartedCount,
            threadStore.BackgroundCount,
            threadStore.PendingCount,
            threadStore.DeadCount);
    }
}
