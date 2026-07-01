// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public static partial class AsyncHelpers
    {
        // WASI is single-threaded but owns its own event loop: the process's
        // main thread IS the pump. WasiEventLoop.PollWasiEventLoopUntilResolved
        // (used throughout System.Private.CoreLib for Task<->Pollable bridging,
        // TimerQueue.Wasi.cs, ThreadPool.Wasi.cs work-item dispatch, and the
        // Mono-WASI samples in src/mono/wasi/testassets/) drains the ThreadPool
        // work queue and the WASI Pollable poll set until the target task
        // completes. Route the compiler-generated async entry point through it
        // instead of the blocking Task.Wait path in AsyncHelpers.NonBrowser.cs,
        // which throws PlatformNotSupportedException from
        // RuntimeFeature.ThrowIfMultithreadingIsNotSupported() on WASI.

        /// <summary>
        /// This method is intended to be used by a compiler-generated async entry point.
        /// </summary>
        /// <remarks>
        /// On WASI, drives the process-owned event loop synchronously until the
        /// task completes; propagates any exception the task faulted with.
        /// </remarks>
        /// <param name="task">The result from the main entry point to await.</param>
        [StackTraceHidden]
        public static void HandleAsyncEntryPoint(Task task)
        {
            WasiEventLoop.PollWasiEventLoopUntilResolvedVoid(task);
        }

        /// <summary>
        /// This method is intended to be used by a compiler-generated async entry point.
        /// </summary>
        /// <remarks>
        /// On WASI, drives the process-owned event loop synchronously until the
        /// task completes; propagates any exception the task faulted with.
        /// </remarks>
        /// <param name="task">The result from the main entry point to await.</param>
        [StackTraceHidden]
        public static int HandleAsyncEntryPoint(Task<int> task)
        {
            return WasiEventLoop.PollWasiEventLoopUntilResolved(task);
        }
    }
}
