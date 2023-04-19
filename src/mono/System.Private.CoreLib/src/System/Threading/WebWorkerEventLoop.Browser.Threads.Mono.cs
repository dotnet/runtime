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

    /// <summary>
    /// A keepalive token prevents a thread from shutting down even if it returns to the JS event
    /// loop.  A thread may want a keepalive token if it needs to allow JS code to run to settle JS
    /// promises or execute JS timeout callbacks.
    /// </summary>
    internal sealed class KeepaliveToken
    {
        public bool Valid {get; private set; }

        private KeepaliveToken() { Valid = true; }

        /// <summary>
        ///  Decrement the Emscripten keepalive count.  A thread with a zero keepalive count will
        ///  terminate when it returns from its start function or from an async invocation from the
        ///  JS event loop.
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
        // hack: threadpool threads are exitable, and nothing else is.
        // see create_thread() in mono/metadata/threads.c
        if (!thread.IsThreadPoolThread)
            throw new InvalidOperationException();
        thread.UnsafeStart();
    }

    /// returns true if the current thread has unsettled JS Interop promises
    private static bool HasUnsettledInteropPromises => HasUnsettledInteropPromisesNative();

    // FIXME: this could be a qcall with a SuppressGCTransitionAttribute
    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern bool HasUnsettledInteropPromisesNative();

    /// <summary>returns true if the current WebWorker has JavaScript objects that depend on the
    /// current managed thread.</summary>
    ///
    /// <remarks>If this returns false, the runtime is allowed to allow the current managed thread
    /// to exit and for the WebWorker to be recycled by Emscripten for another managed
    /// thread.</remarks>
    internal static bool HasJavaScriptInteropDependents
    {
        //
        // FIXME:
        // https://github.com/dotnet/runtime/issues/85052 - unsettled promises are not the only relevant
        // reasons for keeping a worker thread alive. We will need to add other conditions here.
        get => HasUnsettledInteropPromises;
    }
}
