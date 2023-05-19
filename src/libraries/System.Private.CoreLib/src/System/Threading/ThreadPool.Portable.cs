// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    //
    // Portable implementation of ThreadPool
    //

    public static partial class ThreadPool
    {
        // Indicates whether the thread pool should yield the thread from the dispatch loop to the runtime periodically so that
        // the runtime may use the thread for processing other work
#if !(TARGET_BROWSER && FEATURE_WASM_THREADS)
        internal static bool YieldFromDispatchLoop => false;
#endif

        private static bool EnsureConfigInitializedCore() => true;

#pragma warning disable IDE0060
        internal static bool CanSetMinIOCompletionThreads(int ioCompletionThreads) => false;
        internal static bool CanSetMaxIOCompletionThreads(int ioCompletionThreads) => false;
#pragma warning restore IDE0060

        [Conditional("unnecessary")]
        internal static void SetMinIOCompletionThreads(int ioCompletionThreads) { }
        [Conditional("unnecessary")]
        internal static void SetMaxIOCompletionThreads(int ioCompletionThreads) { }
    }
}
