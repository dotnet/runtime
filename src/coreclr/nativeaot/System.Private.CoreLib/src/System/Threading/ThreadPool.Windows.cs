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

        internal static void InitializeForThreadPoolThread() => WindowsThreadPool.InitializeForThreadPoolThread();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void IncrementCompletedWorkItemCount() => WindowsThreadPool.IncrementCompletedWorkItemCount();

        internal static object GetOrCreateThreadLocalCompletionCountObject() => WindowsThreadPool.GetOrCreateThreadLocalCompletionCountObject();

        public static bool SetMaxThreads(int workerThreads, int completionPortThreads) => WindowsThreadPool.SetMaxThreads(workerThreads, completionPortThreads);

        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads) => WindowsThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);

        public static bool SetMinThreads(int workerThreads, int completionPortThreads) => WindowsThreadPool.SetMinThreads(workerThreads, completionPortThreads);

        public static void GetMinThreads(out int workerThreads, out int completionPortThreads) => WindowsThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);

        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads) => WindowsThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);

        /// <summary>
        /// Gets the number of thread pool threads that currently exist.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of threads, the count includes all types.
        /// </remarks>
        public static int ThreadCount => WindowsThreadPool.ThreadCount;

        /// <summary>
        /// Gets the number of work items that have been processed so far.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of work items, the count includes all types.
        /// </remarks>
        public static long CompletedWorkItemCount => WindowsThreadPool.CompletedWorkItemCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void NotifyWorkItemProgress() => WindowsThreadPool.NotifyWorkItemProgress();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool NotifyWorkItemComplete(object threadLocalCompletionCountObject, int _ /*currentTimeMs*/) => WindowsThreadPool.NotifyWorkItemComplete(threadLocalCompletionCountObject, _);

        internal static bool NotifyThreadBlocked() => WindowsThreadPool.NotifyThreadBlocked();
        internal static void NotifyThreadUnblocked() => WindowsThreadPool.NotifyThreadUnblocked();

        internal static unsafe void RequestWorkerThread() => WindowsThreadPool.RequestWorkerThread();

        private static RegisteredWaitHandle RegisterWaitForSingleObject(
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce,
             bool flowExecutionContext)
        {
            return WindowsThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext);
        }

            [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public static unsafe bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped) => WindowsThreadPool.UnsafeQueueNativeOverlapped(overlapped);

        [Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
        [SupportedOSPlatform("windows")]
        public static bool BindHandle(IntPtr osHandle) => WindowsThreadPool.BindHandle(osHandle);

        [SupportedOSPlatform("windows")]
        public static bool BindHandle(SafeHandle osHandle) => WindowsThreadPool.BindHandle(osHandle);
    }
}
