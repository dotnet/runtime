// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Threading;

// <summary>
// This class provides a way for browser threads to asynchronously wait for a sempahore
// from JS, without using the threadpool.  It is used to implement threadpool workers.
// </summary>
[StructLayout(LayoutOptions.Sequential)]
internal partial class LowLevelJSSemaphore : IDisposable
{
    private IntPtr lifo_semaphore;

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern IntPtr InitInternal();

#pragma warning disable IDE0060
    private void Create(int maximumSignalCount)
    {
        lifo_semaphore = InitInternal();
    }
#pragma warning restore IDE0060

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void DeleteInternal(IntPtr semaphore);

    public void Dispose()
    {
        DeleteInternal(lifo_semaphore);
        lifo_semaphore = IntPtr.Zero;
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void ReleaseInternal(IntPtr semaphore, int count);

    internal void Release(int additionalCount)
    {
        ReleaseInternal(lifo_semaphore, count);
    }
    
    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void PrepareWaitInternal(IntPtr semaphore,
                                                   int timeout_ms,
                                                   delegate unmanaged*<IntPtr, GCHandle, IntPtr, void> success_cb,
                                                   delegate unmanaged*<IntPtr, GCHandle, IntPtr, void> timeout_cb,
                                                   GCHandle object,
                                                   IntPtr user_data);

    private class WaitEntry
    {
        public WaitEntry(LowLevelJSSemaphore semaphore, Action<LowLevelJSSemaphore, object?> onSuccess, Action<LowLevelJSSemaphore, object?> onTimeout, object? state)
        {
            OnSuccess = onSuccess;
            OnTimeout = onTimeout;
            Semaphore = semaphore;
            State = state;
        }
        public object? State {get; init; }
        public Action<LowLevelJSSemaphore, object?> OnSuccess {get; init;}
        public Action<LowLevelJSSemaphore, object?> OnTimeout {get; init;}
        public LowLevelJSSemaphore Semaphore {get; init;}
    }

    internal void PrepareWait(int timeout_ms, Action<LowLevelJSSemaphore, object?> onSuccess, Action<LowLeelJSSemaphore, object?> onTimeout, object? state)
    {
        WaitEntry entry = new (this, onSuccess, onTimeout, state);
        GCHandle gchandle = GCHandle.Alloc (entry);
        PrepareWaitInternal (lifo_semaphore, timeout_ms, &SuccessCallback, &TimeoutCallback, gchandle, IntPtr.Zero);
    }

    private static void SuccessCallback(IntPtr lifo_semaphore, GCHandle gchandle, IntPtr user_data)
    {
        WaitEntry entry = (WaitEntry)gchandle.Target!;
        GCHandle.Free(gchandle);
        entry.OnSuccess(entry.Semaphore, entry.State);
    }

    private static void TimeoutCallback(IntPtr lifo_semaphore, GCHandle gchandle, IntPtr user_data)
    {
        WaitEntry entry = (WaitEntry)gchandle.Target!;
        GCHandle.Free(gchandle);
        entry.OnTimeout(entry.Semaphore, entry.State);
    }

}
