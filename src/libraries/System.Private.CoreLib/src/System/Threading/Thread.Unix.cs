// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace System.Threading
{
    public sealed partial class Thread
    {
        // these methods are temporarily accessed via UnsafeAccessor from generated code until we have it in public API, probably in WASI preview3 and promises
#if TARGET_WASI
        internal static System.Threading.Tasks.Task RegisterWasiPollableHandle(int handle, bool ownsPollable, CancellationToken cancellationToken)
        {
            return WasiEventLoop.RegisterWasiPollableHandle(handle, ownsPollable, cancellationToken);
        }

        internal static void RegisterWasiPollHook(object? state, Func<object?, IList<int>> beforePollHook, Action<object?> onResolveCallback, CancellationToken cancellationToken)
        {
            WasiEventLoop.RegisterWasiPollHook(state, beforePollHook, onResolveCallback, cancellationToken);
        }

        internal static T PollWasiEventLoopUntilResolved<T>(Task<T> mainTask)
        {
            return WasiEventLoop.PollWasiEventLoopUntilResolved<T>(mainTask);
        }

        internal static void PollWasiEventLoopUntilResolvedVoid(Task mainTask)
        {
            WasiEventLoop.PollWasiEventLoopUntilResolvedVoid(mainTask);
        }
#endif // TARGET_WASI

        // the closest analog to Sleep(0) on Unix is sched_yield
        internal static void UninterruptibleSleep0() => Thread.Yield();

        private static void SleepInternal(int millisecondsTimeout) => WaitSubsystem.Sleep(millisecondsTimeout);

#if !MONO
        private bool JoinInternal(int millisecondsTimeout)
        {
            // This method assumes the thread has been started
            Debug.Assert((ThreadState & ThreadState.Unstarted) == 0 || (millisecondsTimeout == 0));
            SafeWaitHandle waitHandle = GetJoinHandle();

            // If an OS thread is terminated and its Thread object is resurrected, waitHandle may be finalized and closed
            if (waitHandle.IsClosed)
            {
                return true;
            }

            // Prevent race condition with the finalizer
            try
            {
                waitHandle.DangerousAddRef();
            }
            catch (ObjectDisposedException)
            {
                return true;
            }

            try
            {
                return WaitSubsystem.Wait(waitHandle.DangerousGetHandle(), millisecondsTimeout, interruptible: false) == WaitHandle.WaitSuccess;
            }
            finally
            {
                waitHandle.DangerousRelease();
            }
        }

        private void SetJoinHandle()
        {
            SafeWaitHandle waitHandle = GetJoinHandle();
            Debug.Assert(!waitHandle.IsClosed);

            waitHandle.DangerousAddRef();
            try
            {
                WaitSubsystem.SetEvent(waitHandle.DangerousGetHandle());
            }
            finally
            {
                waitHandle.DangerousRelease();
            }
        }
#endif

        // sched_getcpu doesn't exist on all platforms. On those it doesn't exist on, the shim returns -1
        internal static int GetCurrentProcessorNumber() => Interop.Sys.SchedGetCpu();
    }
}
