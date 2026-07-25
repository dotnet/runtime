// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    public static partial class ThreadPool
    {
        // Indicates that the threadpool should yield the thread from the dispatch loop to the
        // runtime periodically.  We use this to return back to the JS event loop so that the JS
        // event queue can be drained
#pragma warning disable IDE0060 // Remove unused parameter
        internal static bool YieldFromDispatchLoop(int currentTickCount) => true;
#pragma warning restore IDE0060
    }
}
