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
    // FIXME: these keepalive calls could be qcalls with a SuppressGCTransitionAttribute
    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void KeepalivePushInternal();
    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void KeepalivePopInternal();
#if false
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void UnwindToJsInternal();
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void ThreadExitInternal();
#endif

    internal sealed class KeepaliveToken
    {
        public bool Valid {get; private set; }

        private KeepaliveToken() { Valid = true; }

        /// <summary>
        ///  Decrement the Emscripten keepalive count.  A thread with
        ///  a zero keepalive count will terminate when it returns
        ///  from its start function or from an async invocation from
        ///  the JS event loop.
        /// </summary>
        internal void Pop() {
            if (!Valid)
                throw new InvalidOperationException();
            Valid = false;
            KeepalivePopInternal();
        }

        internal static KeepaliveToken Create()
        {
            KeepalivePushInternal();
            return new KeepaliveToken();
        }
    }

    /// <summary>
    ///  Increment the Emscripten keepalive count.  A thread with a positive keepalive can return from its
    ///  thread start function or a JS event loop invocation and continue running in the JS event
    ///  loop.
    /// </summary>
    internal static KeepaliveToken KeepalivePush() => KeepaliveToken.Create();

    // FIXME: these are dangerous they will not unwind managad frames (so finally clauses wont' run) and maybe leak in the interpreter memory
#if false
    /// <summary>
    ///   Abort the current execution and unwind to the JS event loop
    /// </summary>
    ///
    // FIXME: we should probably setup some managed exception to
    // unwind the managed stack before calling the emscripten
    // unwind_to_js to unwind the native stack.
    [DoesNotReturn]
    internal static void UnwindToJs() => UnwindToJsInternal();

    /// <summary>
    /// Terminate the current thread, even if the thread was kept alive with KeepalivePush
    /// </summary>
    internal static void ThreadExit() => ThreadExitInternal();
#endif


    internal static void StartExitable(Thread thread, bool captureContext)
    {
        // don't support captureContext == true, for now, since it's
        // not needed by PortableThreadPool.WorkerThread
        if (captureContext)
            throw new InvalidOperationException();
        // hack: threadpool threads are exitable, and nothing else is.
        // see create_thread() in mono/metadata/threads.c
        if (!thread.IsThreadPoolThread)
            throw new InvalidOperationException();
        thread.UnsafeStart();
    }
}
