// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading;

// <summary>
// This class provides a way for browser threads to asynchronously wait for a semaphore
// from JS, without using the threadpool.  It is used to implement threadpool workers.
// </summary>
internal sealed partial class LowLevelLifoAsyncWaitSemaphore : LowLevelLifoSemaphoreBase, IDisposable
{
    private IntPtr lifo_semaphore;

    [MethodImplAttribute(MethodImplOptions.InternalCall)]
    private static extern IntPtr InitInternal();

    public LowLevelLifoAsyncWaitSemaphore(int initialSignalCount, int maximumSignalCount, int spinCount, Action onWait)
        : base (initialSignalCount, maximumSignalCount, spinCount, onWait)
    {
        CreateAsyncWait(maximumSignalCount);
    }

#pragma warning disable IDE0060
    private void CreateAsyncWait(int maximumSignalCount)
#pragma warning restore IDE0060
    {
        lifo_semaphore = InitInternal();
    }

    [MethodImplAttribute(MethodImplOptions.InternalCall)]
    private static extern void DeleteInternal(IntPtr semaphore);

    public void Dispose()
    {
        DeleteInternal(lifo_semaphore);
        lifo_semaphore = IntPtr.Zero;
    }

    [MethodImplAttribute(MethodImplOptions.InternalCall)]
    private static extern void ReleaseInternal(IntPtr semaphore, int count);

    protected override void ReleaseCore(int count)
    {
        ReleaseInternal(lifo_semaphore, count);
    }

    private sealed record WaitEntry (LowLevelLifoAsyncWaitSemaphore Semaphore, Action<LowLevelLifoAsyncWaitSemaphore, object?> OnSuccess, Action<LowLevelLifoAsyncWaitSemaphore, object?> OnTimeout, object? State)
    {
        public int TimeoutMs {get; internal set;}
        public int StartWaitTicks {get; internal set; }
    }

    public void PrepareAsyncWait(int timeoutMs, Action<LowLevelLifoAsyncWaitSemaphore, object?> onSuccess, Action<LowLevelLifoAsyncWaitSemaphore, object?> onTimeout, object? state)
    {
        Debug.Assert(timeoutMs >= -1);

        // Try to acquire the semaphore or
        // a) register as a waiter and timeoutMs > 0
        // b) bail out if timeoutMs == 0 and return false
        Counts counts = _separated._counts;
        while (true)
        {
            Debug.Assert(counts.SignalCount <= _maximumSignalCount);
            Counts newCounts = counts;
            if (counts.SignalCount != 0)
            {
                newCounts.DecrementSignalCount();
            }
            else if (timeoutMs != 0)
            {
                // Maximum number of spinners reached, register as a waiter instead
                newCounts.IncrementWaiterCount();
            }

            Counts countsBeforeUpdate = _separated._counts.InterlockedCompareExchange(newCounts, counts);
            if (countsBeforeUpdate == counts)
            {
                if (counts.SignalCount != 0)
                {
                    onSuccess (this, state);
                    return;
                }
                if (newCounts.WaiterCount != counts.WaiterCount)
                {
                    PrepareAsyncWaitForSignal(timeoutMs, onSuccess, onTimeout, state);
                    return;
                }
                if (timeoutMs == 0)
                {
                    onTimeout (this, state);
                    return;
                }
                break;
            }

            counts = countsBeforeUpdate;
        }

        Debug.Fail("unreachable");
    }

    private void PrepareAsyncWaitForSignal(int timeoutMs, Action<LowLevelLifoAsyncWaitSemaphore, object?> onSuccess, Action<LowLevelLifoAsyncWaitSemaphore, object?> onTimeout, object? state)
    {
        Debug.Assert(timeoutMs > 0 || timeoutMs == -1);

        _onWait();

        WaitEntry we = new WaitEntry(this, onSuccess, onTimeout, state)
        {
            TimeoutMs = timeoutMs,
            StartWaitTicks = timeoutMs != -1 ? Environment.TickCount : 0,
        };
        PrepareAsyncWaitCore(we);
        // on success calls InternalAsyncWaitSuccess, on timeout calls InternalAsyncWaitTimeout
    }

    private static void InternalAsyncWaitTimeout(LowLevelLifoAsyncWaitSemaphore self, WaitEntry internalWaitEntry)
    {
        WaitEntry we = internalWaitEntry!;
        // Unregister the waiter. The wait subsystem used above guarantees that a thread that wakes due to a timeout does
        // not observe a signal to the object being waited upon.
        self._separated._counts.InterlockedDecrementWaiterCount();
        we.OnTimeout(self, we.State);
    }

    private static void InternalAsyncWaitSuccess(LowLevelLifoAsyncWaitSemaphore self, WaitEntry internalWaitEntry)
    {
        WaitEntry we = internalWaitEntry!;
        int endWaitTicks = we.TimeoutMs != -1 ? Environment.TickCount : 0;
        // Unregister the waiter if this thread will not be waiting anymore, and try to acquire the semaphore
        Counts counts = self._separated._counts;
        while (true)
        {
            Debug.Assert(counts.WaiterCount != 0);
            Counts newCounts = counts;
            if (counts.SignalCount != 0)
            {
                newCounts.DecrementSignalCount();
                newCounts.DecrementWaiterCount();
            }

            // This waiter has woken up and this needs to be reflected in the count of waiters signaled to wake
            if (counts.CountOfWaitersSignaledToWake != 0)
            {
                newCounts.DecrementCountOfWaitersSignaledToWake();
            }

            Counts countsBeforeUpdate = self._separated._counts.InterlockedCompareExchange(newCounts, counts);
            if (countsBeforeUpdate == counts)
            {
                if (counts.SignalCount != 0)
                {
                    we.OnSuccess(self, we.State);
                    return;
                }
                break;
            }

            counts = countsBeforeUpdate;
        }
        // if we get here, we need to keep waiting because the SignalCount above was 0 after we did
        // the CompareExchange - someone took the signal before us.

        if (we.TimeoutMs != -1) {
            int waitMs = endWaitTicks - we.StartWaitTicks;
            if (waitMs >= 0 && waitMs < we.TimeoutMs)
                we.TimeoutMs -= waitMs;
            else
                we.TimeoutMs = 0;
            we.StartWaitTicks = endWaitTicks;
        }
        PrepareAsyncWaitCore (we);
        // on success calls InternalAsyncWaitSuccess, on timeout calls InternalAsyncWaitTimeout
    }

    private static void PrepareAsyncWaitCore(WaitEntry internalWaitEntry)
    {
        int timeoutMs = internalWaitEntry.TimeoutMs;
        LowLevelLifoAsyncWaitSemaphore semaphore = internalWaitEntry.Semaphore;
        if (timeoutMs == 0) {
            internalWaitEntry.OnTimeout (semaphore, internalWaitEntry.State);
            return;
        }
        GCHandle gchandle = GCHandle.Alloc (internalWaitEntry);
        unsafe {
            delegate* unmanaged<IntPtr, IntPtr, void> successCallback = &SuccessCallback;
            delegate* unmanaged<IntPtr, IntPtr, void> timeoutCallback = &TimeoutCallback;
            PrepareAsyncWaitInternal (semaphore.lifo_semaphore, timeoutMs, successCallback, timeoutCallback, GCHandle.ToIntPtr(gchandle));
        }
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern unsafe void PrepareAsyncWaitInternal(IntPtr semaphore,
                                                               int timeoutMs,
                                                               /*delegate* unmanaged<IntPtr, IntPtr, void> successCallback*/ void* successCallback,
                                                               /*delegate* unmanaged<IntPtr, IntPtr, void> timeoutCallback*/ void* timeoutCallback,
                                                               IntPtr userData);

    [UnmanagedCallersOnly]
    private static void SuccessCallback(IntPtr lifoSemaphore, IntPtr userData)
    {
        GCHandle gchandle = GCHandle.FromIntPtr(userData);
        WaitEntry internalWaitEntry = (WaitEntry)gchandle.Target!;
        gchandle.Free();
        InternalAsyncWaitSuccess(internalWaitEntry.Semaphore, internalWaitEntry);
    }

    [UnmanagedCallersOnly]
    private static void TimeoutCallback(IntPtr lifoSemaphore, IntPtr userData)
    {
        GCHandle gchandle = GCHandle.FromIntPtr(userData);
        WaitEntry internalWaitEntry = (WaitEntry)gchandle.Target!;
        gchandle.Free();
        InternalAsyncWaitTimeout(internalWaitEntry.Semaphore, internalWaitEntry);
    }

}
