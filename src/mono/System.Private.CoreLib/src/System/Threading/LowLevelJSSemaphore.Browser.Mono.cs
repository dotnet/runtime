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
internal sealed partial class LowLevelLifoSemaphore : IDisposable
{
    internal static LowLevelLifoSemaphore CreateAsyncJS (int initialSignalCount, int maximumSignalCount, int spinCount, Action onWait)
    {
        return new LowLevelLifoSemaphore(initialSignalCount, maximumSignalCount, spinCount, onWait, asyncJS: true);
    }

    private LowLevelLifoSemaphore(int initialSignalCount, int maximumSignalCount, int spinCount, Action onWait, bool asyncJS)
    {
        Debug.Assert(initialSignalCount >= 0);
        Debug.Assert(initialSignalCount <= maximumSignalCount);
        Debug.Assert(maximumSignalCount > 0);
        Debug.Assert(spinCount >= 0);
        Debug.Assert(asyncJS);

        _separated = default;
        _separated._counts.SignalCount = (uint)initialSignalCount;
        _maximumSignalCount = maximumSignalCount;
        _spinCount = spinCount;
        _onWait = onWait;

        CreateAsyncJS(maximumSignalCount);
    }

#pragma warning disable IDE0060
    private void CreateAsyncJS(int maximumSignalCount)
    {
        _kind = LifoSemaphoreKind.AsyncJS;
        lifo_semaphore = InitInternal((int)_kind);
    }
#pragma warning restore IDE0060

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern unsafe void PrepareAsyncWaitInternal(IntPtr semaphore,
                                                               int timeoutMs,
                                                               /*delegate* unmanaged<IntPtr, IntPtr, void> successCallback*/ void* successCallback,
                                                               /*delegate* unmanaged<IntPtr, IntPtr, void> timeoutCallback*/ void* timeoutCallback,
                                                               IntPtr userData);

    private sealed record WaitEntry (LowLevelLifoSemaphore Semaphore, Action<LowLevelLifoSemaphore, object?> OnSuccess, Action<LowLevelLifoSemaphore, object?> OnTimeout, object? State);

    internal void PrepareAsyncWait(int timeoutMs, Action<LowLevelLifoSemaphore, object?> onSuccess, Action<LowLevelLifoSemaphore, object?> onTimeout, object? state)
    {
        //FIXME(ak): the async wait never spins. Shoudl we spin a little?
        Debug.Assert(timeoutMs >= -1);

        // Try to acquire the semaphore or
        // [[a) register as a spinner if false and timeoutMs > 0]]
        // b) register as a waiter if [[there's already too many spinners or]] true and timeoutMs > 0
        // c) bail out if timeoutMs == 0 and return false
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

#if false
        // Unregister as spinner, and acquire the semaphore or register as a waiter
        counts = _separated._counts;
        while (true)
        {
            Counts newCounts = counts;
            if (counts.SignalCount != 0)
            {
                newCounts.DecrementSignalCount();
            }
            else
            {
                newCounts.IncrementWaiterCount();
            }

            Counts countsBeforeUpdate = _separated._counts.InterlockedCompareExchange(newCounts, counts);
            if (countsBeforeUpdate == counts)
            {
                return counts.SignalCount != 0 || WaitForSignal(timeoutMs);
            }

            counts = countsBeforeUpdate;
        }
#endif
    }

    private void PrepareAsyncWaitForSignal(int timeoutMs, Action<LowLevelLifoSemaphore, object?> onSuccess, Action<LowLevelLifoSemaphore, object?> onTimeout, object? state)
    {
        Debug.Assert(timeoutMs > 0 || timeoutMs == -1);

        _onWait();

        PrepareAsyncWaitCore(timeoutMs, s_InternalAsyncWaitSuccess, s_InternalAsyncWaitTimeout, new InternalWait(timeoutMs, onSuccess, onTimeout, state));
    }

    private static readonly Action<LowLevelLifoSemaphore, object?> s_InternalAsyncWaitSuccess = InternalAsyncWaitSuccess;

    private static readonly Action<LowLevelLifoSemaphore, object?> s_InternalAsyncWaitTimeout = InternalAsyncWaitTimeout;

    internal sealed record InternalWait(int TimeoutMs, Action<LowLevelLifoSemaphore, object?> OnSuccess, Action<LowLevelLifoSemaphore, object?> OnTimeout, object? State);

    private static void InternalAsyncWaitTimeout(LowLevelLifoSemaphore self, object? internalWaitObj)
    {
        InternalWait i = (InternalWait)internalWaitObj!;
        // Unregister the waiter. The wait subsystem used above guarantees that a thread that wakes due to a timeout does
        // not observe a signal to the object being waited upon.
        self._separated._counts.InterlockedDecrementWaiterCount();
        i.OnTimeout(self, i.State);
    }

    private static void InternalAsyncWaitSuccess(LowLevelLifoSemaphore self, object? internalWaitObj)
    {
        InternalWait i = (InternalWait)internalWaitObj!;
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
                    i.OnSuccess(self, i.State);
                    return;
                }
                break;
            }

            counts = countsBeforeUpdate;
        }
        // if we get here, we need to keep waiting because the SignalCount above was 0 after we did
        // the CompareExchange - someone took the signal before us.
        // FIXME(ak): why is the timeoutMs the same as before? wouldn't we starve? why does LowLevelLifoSemaphore.WaitForSignal not decrement timeoutMs?
        self.PrepareAsyncWaitCore (i.TimeoutMs, s_InternalAsyncWaitSuccess, s_InternalAsyncWaitTimeout, i);
    }

    internal void PrepareAsyncWaitCore(int timeout_ms, Action<LowLevelLifoSemaphore, object?> onSuccess, Action<LowLevelLifoSemaphore, object?> onTimeout, object? state)
    {
        ThrowIfInvalidSemaphoreKind (LifoSemaphoreKind.AsyncJS);
        WaitEntry entry = new (this, onSuccess, onTimeout, state);
        GCHandle gchandle = GCHandle.Alloc (entry);
        unsafe {
            delegate* unmanaged<IntPtr, IntPtr, void> successCallback = &SuccessCallback;
            delegate* unmanaged<IntPtr, IntPtr, void> timeoutCallback = &TimeoutCallback;
            PrepareAsyncWaitInternal (lifo_semaphore, timeout_ms, successCallback, timeoutCallback, GCHandle.ToIntPtr(gchandle));
        }
    }

    [UnmanagedCallersOnly]
    private static void SuccessCallback(IntPtr lifoSemaphore, IntPtr userData)
    {
        GCHandle gchandle = GCHandle.FromIntPtr(userData);
        WaitEntry entry = (WaitEntry)gchandle.Target!;
        gchandle.Free();
        entry.OnSuccess(entry.Semaphore, entry.State);
    }

    [UnmanagedCallersOnly]
    private static void TimeoutCallback(IntPtr lifoSemaphore, IntPtr userData)
    {
        GCHandle gchandle = GCHandle.FromIntPtr(userData);
        WaitEntry entry = (WaitEntry)gchandle.Target!;
        gchandle.Free();
        entry.OnTimeout(entry.Semaphore, entry.State);
    }

}
