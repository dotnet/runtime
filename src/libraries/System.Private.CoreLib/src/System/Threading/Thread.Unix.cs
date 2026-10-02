// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public sealed partial class Thread
    {
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
