// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Threading.Tasks;

namespace System.Threading
{
    public sealed partial class Thread
    {
        // these methods are temporarily accessed via UnsafeAccessor from generated code until we have it in public API, probably in WASI preview3 and promises
#if TARGET_WASI
        internal static System.Threading.Tasks.Task RegisterWasiPollableHandle(int handle)
        {
            return WasiEventLoop.RegisterWasiPollableHandle(handle);
        }

        internal static int PollWasiEventLoopUntilResolved(Task<int> mainTask)
        {
            while (!mainTask.IsCompleted)
            {
                WasiEventLoop.DispatchWasiEventLoop();
            }
            var exception = mainTask.Exception;
            if (exception is not null)
            {
                throw exception;
            }

            return mainTask.Result;
        }

#endif

        // the closest analog to Sleep(0) on Unix is sched_yield
        internal static void UninterruptibleSleep0() => Thread.Yield();

#if !CORECLR
        private static void SleepInternal(int millisecondsTimeout) => WaitSubsystem.Sleep(millisecondsTimeout);
#endif

        // sched_getcpu doesn't exist on all platforms. On those it doesn't exist on, the shim returns -1
        internal static int GetCurrentProcessorNumber() => Interop.Sys.SchedGetCpu();
    }
}
