// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    //
    // Windows-specific implementation of ThreadPool
    //
    internal static class WindowsThreadPool
    {
        /// <summary>
        /// The maximum number of threads in the default thread pool on Windows 10 as computed by
        /// TppComputeDefaultMaxThreads(TppMaxGlobalPool).
        /// </summary>
        /// <remarks>
        /// Note that Windows 8 and 8.1 used a different value: Math.Max(4 * Environment.ProcessorCount, 512).
        /// </remarks>
        private static readonly int MaxThreadCount = Math.Max(8 * Environment.ProcessorCount, 768);

        private static IntPtr s_work;

        [StructLayout(LayoutKind.Sequential)]
        private struct CacheLineSeparated
        {
            private readonly Internal.PaddingFor32 pad1;

            // This flag is used for communication between item enqueuing and workers that process the items.
            // There are two states of this flag:
            // 0: has no guarantees
            // 1: means a worker will check work queues and ensure that
            //    any work items inserted in work queue before setting the flag
            //    are picked up.
            //    Note: The state must be cleared by the worker thread _before_
            //       checking. Otherwise there is a window between finding no work
            //       and resetting the flag, when the flag is in a wrong state.
            //       A new work item may be added right before the flag is reset
            //       without asking for a worker, while the last worker is quitting.
            public int _hasOutstandingThreadRequest;

            private readonly Internal.PaddingFor32 pad2;
        }

        private static CacheLineSeparated _separated;

        private sealed class ThreadCountHolder
        {
            internal ThreadCountHolder() => Interlocked.Increment(ref s_threadCount);
            ~ThreadCountHolder() => Interlocked.Decrement(ref s_threadCount);
        }

        [ThreadStatic]
        private static ThreadCountHolder? t_threadCountHolder;
        private static int s_threadCount;

        [StructLayout(LayoutKind.Sequential)]
        private struct WorkingThreadCounter
        {
            private readonly Internal.PaddingFor32 pad1;

            public volatile int Count;

            private readonly Internal.PaddingFor32 pad2;
        }

        // The number of threads executing work items in the Dispatch method
        private static WorkingThreadCounter s_workingThreadCounter;

        private static readonly ThreadInt64PersistentCounter s_completedWorkItemCounter = new ThreadInt64PersistentCounter();

        [ThreadStatic]
        private static ThreadInt64PersistentCounter.ThreadLocalNode? t_completionCountNode;

        internal static void InitializeForThreadPoolThread() => t_threadCountHolder = new ThreadCountHolder();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void IncrementCompletedWorkItemCount() => GetOrCreateThreadLocalCompletionCountNode().Increment();

        internal static ThreadInt64PersistentCounter.ThreadLocalNode GetOrCreateThreadLocalCompletionCountNode() =>
            t_completionCountNode ?? CreateThreadLocalCompletionCountNode();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ThreadInt64PersistentCounter.ThreadLocalNode CreateThreadLocalCompletionCountNode()
        {
            Debug.Assert(t_completionCountNode == null);

            ThreadInt64PersistentCounter.ThreadLocalNode threadLocalCompletionCountNode = s_completedWorkItemCounter.CreateThreadLocalCountObject();
            t_completionCountNode = threadLocalCompletionCountNode;
            return threadLocalCompletionCountNode;
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public static bool SetMaxThreads(int workerThreads, int completionPortThreads)
        {
            // Not supported at present
            return false;
        }
#pragma warning restore IDE0060

        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
        {
            // Note that worker threads and completion port threads share the same thread pool.
            // The total number of threads cannot exceed MaxThreadCount.
            workerThreads = MaxThreadCount;
            completionPortThreads = MaxThreadCount;
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public static bool SetMinThreads(int workerThreads, int completionPortThreads)
        {
            // Not supported at present
            return false;
        }
#pragma warning restore IDE0060

        public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
        {
            workerThreads = 0;
            completionPortThreads = 0;
        }

        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
        {
            // Make sure we return a non-negative value if thread pool defaults are changed
            int availableThreads = Math.Max(MaxThreadCount - s_workingThreadCounter.Count, 0);

            workerThreads = availableThreads;
            completionPortThreads = availableThreads;
        }

        /// <summary>
        /// Gets the number of thread pool threads that currently exist.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of threads, the count includes all types.
        /// </remarks>
        public static int ThreadCount => s_threadCount;

        /// <summary>
        /// Gets the number of work items that have been processed so far.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of work items, the count includes all types.
        /// </remarks>
        public static long CompletedWorkItemCount => s_completedWorkItemCounter.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void NotifyWorkItemProgress() => IncrementCompletedWorkItemCount();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool NotifyWorkItemComplete(ThreadInt64PersistentCounter.ThreadLocalNode threadLocalCompletionCountNode, int _ /*currentTimeMs*/)
        {
            threadLocalCompletionCountNode.Increment();
            return true;
        }

        internal static bool NotifyThreadBlocked() { return false; }
        internal static void NotifyThreadUnblocked() { }

        [UnmanagedCallersOnly]
        private static void DispatchCallback(IntPtr instance, IntPtr context, IntPtr work)
        {
            var wrapper = ThreadPoolCallbackWrapper.Enter();

            Debug.Assert(s_work == work);
            // Before looking for work items, acknowledge that the thread request has been satisfied
            _separated._hasOutstandingThreadRequest = 0;
            // NOTE: the thread request must be cleared before doing Dispatch.
            //       the following Interlocked.Increment will guarantee the ordering.
            Interlocked.Increment(ref s_workingThreadCounter.Count);
            ThreadPoolWorkQueue.Dispatch();
            Interlocked.Decrement(ref s_workingThreadCounter.Count);

            // We reset the thread after executing each callback
            wrapper.Exit(resetThread: false);
        }

        internal static void EnsureWorkerRequested()
        {
            // Only one worker is requested at a time to mitigate Thundering Herd problem.
            if (_separated._hasOutstandingThreadRequest == 0 &&
                Interlocked.Exchange(ref _separated._hasOutstandingThreadRequest, 1) == 0)
            {
                RequestWorkerThread();
            }
        }

        private static unsafe void RequestWorkerThread()
        {
            if (s_work == IntPtr.Zero)
            {
                IntPtr work = Interop.Kernel32.CreateThreadpoolWork(&DispatchCallback, IntPtr.Zero, IntPtr.Zero);
                if (work == IntPtr.Zero)
                    throw new OutOfMemoryException();

                if (Interlocked.CompareExchange(ref s_work, work, IntPtr.Zero) != IntPtr.Zero)
                    Interop.Kernel32.CloseThreadpoolWork(work);
            }

            Interop.Kernel32.SubmitThreadpoolWork(s_work);
        }

        internal static RegisteredWaitHandle RegisterWaitForSingleObject(
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object? state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce,
             bool flowExecutionContext)
        {
            ArgumentNullException.ThrowIfNull(waitObject);
            ArgumentNullException.ThrowIfNull(callBack);

            var callbackHelper = new _ThreadPoolWaitOrTimerCallback(callBack, state, flowExecutionContext);
            var registeredWaitHandle = new RegisteredWaitHandle(waitObject.SafeWaitHandle, callbackHelper, millisecondsTimeOutInterval, !executeOnlyOnce);

            registeredWaitHandle.RestartWait();
            return registeredWaitHandle;
        }

        private static unsafe void NativeOverlappedCallback(nint overlappedPtr)
        {
            if (NativeRuntimeEventSource.Log.IsEnabled())
                NativeRuntimeEventSource.Log.ThreadPoolIODequeue((NativeOverlapped*)overlappedPtr);

            IOCompletionCallbackHelper.PerformSingleIOCompletionCallback(0, 0, (NativeOverlapped*)overlappedPtr);
        }

        [SupportedOSPlatform("windows")]
        public static unsafe bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped)
        {
            if (overlapped == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.overlapped);
            }

            // OS doesn't signal handle, so do it here
            overlapped->InternalLow = (IntPtr)0;

            if (NativeRuntimeEventSource.Log.IsEnabled())
                NativeRuntimeEventSource.Log.ThreadPoolIOEnqueue(overlapped);

            // Both types of callbacks are executed on the same thread pool
            return ThreadPool.UnsafeQueueUserWorkItem(NativeOverlappedCallback, (nint)overlapped, preferLocal: false);
        }

        [Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
        [SupportedOSPlatform("windows")]
        public static bool BindHandle(IntPtr osHandle)
        {
            throw new PlatformNotSupportedException(SR.Arg_PlatformNotSupported); // Replaced by ThreadPoolBoundHandle.BindHandle
        }

        [SupportedOSPlatform("windows")]
        public static bool BindHandle(SafeHandle osHandle)
        {
            throw new PlatformNotSupportedException(SR.Arg_PlatformNotSupported); // Replaced by ThreadPoolBoundHandle.BindHandle
        }
    }
}
