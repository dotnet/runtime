// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !MONO
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#endif

namespace System.Threading
{
    // Bridges the CoreCLR finalizer machinery to the WASI event loop.
    // EnableFinalization runs inside the GC, so it cannot re-enter managed
    // code or the ThreadPool directly. Instead the native side sets an
    // atomic flag (WasiFinalizer_Schedule) that WasiEventLoop drains at a
    // safe point via TryClearPending + RunWorker.
    internal static partial class WasiFinalizerScheduler
    {
#if MONO
        // Mono WASI drives finalization through its own runtime machinery and
        // does not expose the WasiFinalizer_* QCalls that CoreCLR uses, so the
        // event-loop drain is a no-op here.
        internal static void DrainIfPending() { }
#else
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "WasiFinalizer_TryClearPending")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool TryClearPendingFinalization();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "WasiFinalizer_RunWorker")]
        internal static partial void ExecuteFinalizationCallback();

        internal static void DrainIfPending()
        {
            if (TryClearPendingFinalization())
            {
                ExecuteFinalizationCallback();
            }
        }
#endif
    }
}
