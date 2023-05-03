// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    //
    // Windows-specific implementation of ThreadPool
    //

    public static partial class ThreadPool
    {
        // Indicates whether the thread pool should yield the thread from the dispatch loop to the runtime periodically so that
        // the runtime may use the thread for processing other work.
        //
        // Windows thread pool threads need to yield back to the thread pool periodically, otherwise those threads may be
        // considered to be doing long-running work and change thread pool heuristics, such as slowing or halting thread
        // injection.
        internal static bool YieldFromDispatchLoop => WindowsThreadPool.YieldFromDispatchLoop;
    }
}
