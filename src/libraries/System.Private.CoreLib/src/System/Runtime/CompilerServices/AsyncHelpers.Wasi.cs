// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public static partial class AsyncHelpers
    {
        // On WASI the process's main thread is the event-loop pump.
        // Route the compiler-generated async entry point through it
        // instead of AsyncHelpers.NonBrowser.cs's GetAwaiter().GetResult(),
        // whose blocking wait throws PNSE on !IsMultithreadingSupported.
        // The poll helper pumps the loop to completion and then propagates the
        // result with await semantics via GetAwaiter().GetResult().

        /// <summary>
        /// This method is intended to be used by a compiler-generated async entry point.
        /// </summary>
        /// <param name="task">The result from the main entry point to await.</param>
        [StackTraceHidden]
        public static void HandleAsyncEntryPoint(Task task)
        {
            WasiEventLoop.PollWasiEventLoopUntilResolvedVoid(task);
        }

        /// <summary>
        /// This method is intended to be used by a compiler-generated async entry point.
        /// </summary>
        /// <param name="task">The result from the main entry point to await.</param>
        [StackTraceHidden]
        public static int HandleAsyncEntryPoint(Task<int> task)
        {
            return WasiEventLoop.PollWasiEventLoopUntilResolved(task);
        }
    }
}
