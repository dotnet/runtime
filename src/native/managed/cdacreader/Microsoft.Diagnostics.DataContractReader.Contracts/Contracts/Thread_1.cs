// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;

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

    [Flags]
    public enum AMD64ContextFlags : uint
    {
        CONTEXT_AMD = 0x00100000,
        CONTEXT_CONTROL = CONTEXT_AMD | 0x00000001,
        CONTEXT_INTEGER = CONTEXT_AMD | 0x00000002,
        CONTEXT_SEGMENTS = CONTEXT_AMD | 0x00000004,
        CONTEXT_FLOATING_POINT = CONTEXT_AMD | 0x00000008,
        CONTEXT_DEBUG_REGISTERS = CONTEXT_AMD | 0x00000010,
        CONTEXT_FULL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT,
        CONTEXT_ALL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS,
        CONTEXT_XSTATE = CONTEXT_AMD | 0x00000040,
        CONTEXT_KERNEL_CET = CONTEXT_AMD | 0x00000080,
    }

    int IThread.GetThreadContext(TargetPointer threadPointer)
    {
        ThreadData threadData = ((IThread)this).GetThreadData(threadPointer);
        int hr;

        FrameIterator.EnumerateFrames(_target, threadData.Frame);

        TargetPointer framePointer = threadData.Frame;
        while (framePointer != new TargetPointer(ulong.MaxValue))
        {
            Data.Frame frame = _target.ProcessedData.GetOrAdd<Data.Frame>(framePointer);
            Console.WriteLine(frame.Type);
            framePointer = frame.Next;
        }

        unsafe
        {
            byte[] bytes = new byte[0x700];

            fixed (byte* ptr = bytes)
            {
                Span<byte> buffer = bytes;
                hr = _target.GetThreadContext((uint)threadData.OSId.Value, (uint)AMD64ContextFlags.CONTEXT_FULL, 0x700, buffer);

                Console.WriteLine();
            }
        }


        return hr;
    }
}
