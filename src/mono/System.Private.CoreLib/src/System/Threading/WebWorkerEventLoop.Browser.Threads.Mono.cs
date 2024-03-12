// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Threading;

/// <summary>
///   Keep a pthread alive in its WebWorker after its pthread start function returns.
/// </summary>
internal static class WebWorkerEventLoop
{
    /// <summary>
    ///   Start a thread that may be kept alive on its webworker after the start function returns,
    ///   if the emscripten keepalive count is positive.  Once the thread returns to the JS event
    ///   loop it will be able to settle JS promises as well as run any queued managed async
    ///   callbacks.
    /// </summary>
    internal static void StartExitable(Thread thread, bool captureContext)
    {
        // don't support captureContext == true, for now, since it's
        // not needed by PortableThreadPool.WorkerThread
        if (captureContext)
            throw new InvalidOperationException();
        // for now, threadpool threads are exitable, and nothing else is.
        if (!thread.IsThreadPoolThread)
            throw new InvalidOperationException();
        thread.HasExternalEventLoop = true;
        thread.UnsafeStart();
    }
}
