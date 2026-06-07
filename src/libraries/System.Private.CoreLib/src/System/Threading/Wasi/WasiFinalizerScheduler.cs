// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    // Bridges the CoreCLR native finalizer thread machinery to the WASI event loop.
    //
    // On WASI there is no separate finalizer thread and no JavaScript event loop
    // to defer work to. Native FinalizerThread::EnableFinalization() is invoked
    // from within the GC, so it cannot run FinalizerThreadWorkerIteration inline
    // (the worker iteration declares GC_TRIGGERS + MODE_COOPERATIVE and switches
    // thread mode via EnablePreemptiveGC, which is unsafe to do mid-collection).
    // It also cannot cross back into managed code to queue work — even a
    // ThreadPool enqueue allocates and acquires locks, which would re-enter the
    // GC currently in progress.
    //
    // The native side instead sets an atomic flag in SystemJS_ScheduleFinalization
    // (just a volatile store, safe inside the GC). WasiEventLoop polls
    // SystemJS_TryClearPendingFinalization between work-queue iterations from
    // PollWasiEventLoopUntilResolved and, when the flag is set, calls
    // SystemJS_ExecuteFinalizationCallback at a safe point.
    internal static unsafe partial class WasiFinalizerScheduler
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "SystemJS_TryClearPendingFinalization")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool TryClearPendingFinalization();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "SystemJS_ExecuteFinalizationCallback")]
        internal static partial void ExecuteFinalizationCallback();

        internal static void DrainIfPending()
        {
            if (TryClearPendingFinalization())
            {
                ExecuteFinalizationCallback();
            }
        }
    }
}
