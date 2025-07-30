// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

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
            _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.FinalizerThread)),
            _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.GCThread)));
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

        TargetPointer address = _target.ReadPointer(thread.ExceptionTracker);
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
            thread.RuntimeThreadLocals?.AllocContext.GCAllocationContext.Pointer ?? TargetPointer.Null,
            thread.RuntimeThreadLocals?.AllocContext.GCAllocationContext.Limit ?? TargetPointer.Null,
            thread.Frame,
            firstNestedException,
            thread.TEB,
            thread.LastThrownObject.Handle,
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
