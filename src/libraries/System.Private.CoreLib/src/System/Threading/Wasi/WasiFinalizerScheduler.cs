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
    //
    // Instead, native EnableFinalization calls SystemJS_ScheduleFinalization,
    // which dispatches to the function pointer registered here at startup.
    // That callback queues SystemJS_ExecuteFinalizationCallback through the
    // ThreadPool, where it is picked up by WasiEventLoop's pump at a safe point.
    //
    // This is the WASI analog of the browser's setTimeout(..., 0) pattern in
    // src/native/libs/System.Native.Browser/native/scheduling.ts.
    internal static unsafe partial class WasiFinalizerScheduler
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "WasiFinalizerScheduler_Register")]
        private static partial void Register(delegate* unmanaged<void> callback);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "SystemJS_ExecuteFinalizationCallback")]
        private static partial void ExecuteFinalizationCallback();

#pragma warning disable CA2255 // ModuleInitializer is used here to register the
                               // scheduler before any GC-driven EnableFinalization
                               // call. In CoreCLR coreclr_initialize triggers the
                               // module initializer of CoreLib early enough that
                               // the function pointer is installed before the GC
                               // begins requesting finalization.

        [ModuleInitializer]
#pragma warning restore CA2255
        internal static void Initialize()
        {
            Register(&ScheduleFinalization);
        }

        [UnmanagedCallersOnly]
        private static void ScheduleFinalization()
        {
            // UnsafeQueueUserWorkItem doesn't flow ExecutionContext; finalization
            // semantically owns its own context anyway.
            ThreadPool.UnsafeQueueUserWorkItem(static _ => ExecuteFinalizationCallback(), state: (object?)null, preferLocal: false);
        }
    }
}
