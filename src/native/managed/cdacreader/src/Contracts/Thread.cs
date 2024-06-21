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

[Flags]
internal enum ThreadState
{
    Unknown             = 0x00000000,
    Hijacked            = 0x00000080,   // Return address has been hijacked
    Background          = 0x00000200,   // Thread is a background thread
    Unstarted           = 0x00000400,   // Thread has never been started
    Dead                = 0x00000800,   // Thread is dead
    ThreadPoolWorker    = 0x01000000,   // Thread is a thread pool worker thread
}

internal record struct ThreadData(
    uint Id,
    TargetNUInt OSId,
    ThreadState State,
    bool PreemptiveGCDisabled,
    TargetPointer AllocContextPointer,
    TargetPointer AllocContextLimit,
    TargetPointer Frame,
    TargetPointer FirstNestedException,
    TargetPointer TEB,
    TargetPointer LastThrownObjectHandle,
    TargetPointer NextThread);

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
    public virtual ThreadData GetThreadData(TargetPointer thread) => throw new NotImplementedException();
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

        // Get the offset into Thread of the SLink. We use this to find the actual
        // first thread from the linked list node contained by the first thread.
        Target.TypeInfo type = _target.GetTypeInfo(DataType.Thread);
        _threadLinkOffset = (ulong)type.Fields[nameof(Data.Thread.LinkNext)].Offset;
    }

    ThreadStoreData IThread.GetThreadStoreData()
    {
        Data.ThreadStore threadStore = _target.ProcessedData.GetOrAdd<Data.ThreadStore>(_threadStoreAddr);
        return new ThreadStoreData(
            threadStore.ThreadCount,
            GetThreadFromLink(threadStore.FirstThreadLink),
            _target.ReadGlobalPointer(Constants.Globals.FinalizerThread),
            _target.ReadGlobalPointer(Constants.Globals.GCThread));
    }

    ThreadStoreCounts IThread.GetThreadCounts()
    {
        Data.ThreadStore threadStore = _target.ProcessedData.GetOrAdd<Data.ThreadStore>(_threadStoreAddr);
        return new ThreadStoreCounts(
            threadStore.UnstartedCount,
            threadStore.BackgroundCount,
            threadStore.PendingCount,
            threadStore.DeadCount);
    }

    ThreadData IThread.GetThreadData(TargetPointer threadPointer)
    {
        Data.Thread thread = _target.ProcessedData.GetOrAdd<Data.Thread>(threadPointer);

        // Exception tracker is a pointer when EH funclets are enabled
        TargetPointer address = _target.ReadGlobal<byte>(Constants.Globals.FeatureEHFunclets) != 0
            ? _target.ReadPointer(thread.ExceptionTracker)
            : thread.ExceptionTracker;
        TargetPointer firstNestedException = TargetPointer.Null;
        if (address != TargetPointer.Null)
        {
            Data.ExceptionInfo exceptionInfo = _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(address);
            firstNestedException = exceptionInfo.PreviousNestedInfo;
        }

        return new ThreadData(
            thread.Id,
            thread.OSId,
            (ThreadState)thread.State,
            (thread.PreemptiveGCDisabled & 0x1) != 0,
            thread.RuntimeThreadLocals?.AllocContext.Pointer ?? TargetPointer.Null,
            thread.RuntimeThreadLocals?.AllocContext.Limit ?? TargetPointer.Null,
            thread.Frame,
            firstNestedException,
            thread.TEB,
            thread.LastThrownObject,
            GetThreadFromLink(thread.LinkNext));
    }

    private TargetPointer GetThreadFromLink(TargetPointer threadLink)
    {
        if (threadLink == TargetPointer.Null)
            return TargetPointer.Null;

        // Get the address of the thread containing the link
        return new TargetPointer(threadLink - _threadLinkOffset);
    }
}
