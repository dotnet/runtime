// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    public static partial class ThreadPool
    {
        [FeatureSwitchDefinition("System.Threading.ThreadPool.UseWindowsThreadPool")]
        internal static bool UseWindowsThreadPool { get; } =
            AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.UseWindowsThreadPool", "DOTNET_ThreadPool_UseWindowsThreadPool");

#pragma warning disable CA1823
        // The field should reflect what the property returns because the property can be stubbed by trimming,
        // such that sos reflects the actual state of what thread pool is being used and not just the config value.
        private static readonly bool s_useWindowsThreadPool = UseWindowsThreadPool; // Name relied on by sos
#pragma warning restore CA1823

#if NATIVEAOT
        private const bool IsWorkerTrackingEnabledInConfig = false;
#else
        private static readonly bool IsWorkerTrackingEnabledInConfig =
            UseWindowsThreadPool ? false : AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.EnableWorkerTracking", "DOTNET_ThreadPool_EnableWorkerTracking");
#endif

        // Indicates whether the thread pool should yield the thread from the dispatch loop to the runtime periodically so that
        // the runtime may use the thread for processing other work.
        internal static bool YieldFromDispatchLoop(int currentTickCount)
        {
            if (UseWindowsThreadPool)
            {
                // Windows thread pool threads need to yield back to the thread pool periodically, otherwise those threads may be
                // considered to be doing long-running work and change thread pool heuristics, such as slowing or halting thread
                // injection.
                return true;
            }

            PortableThreadPool.ThreadPoolInstance.NotifyDispatchProgress(currentTickCount);
            return false;
        }

        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public static unsafe bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped) =>
            ThreadPool.UseWindowsThreadPool ?
            WindowsThreadPool.UnsafeQueueNativeOverlapped(overlapped) :
            UnsafeQueueNativeOverlappedPortableCore(overlapped);

        [Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
        [SupportedOSPlatform("windows")]
        public static bool BindHandle(IntPtr osHandle) =>
            ThreadPool.UseWindowsThreadPool ?
            WindowsThreadPool.BindHandle(osHandle) :
            BindHandlePortableCore(osHandle);

        [SupportedOSPlatform("windows")]
        public static bool BindHandle(SafeHandle osHandle) =>
            ThreadPool.UseWindowsThreadPool ?
            WindowsThreadPool.BindHandle(osHandle) :
            BindHandlePortableCore(osHandle);

        internal static void InitializeForThreadPoolThread()
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                WindowsThreadPool.InitializeForThreadPoolThread();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void IncrementCompletedWorkItemCount() => WindowsThreadPool.IncrementCompletedWorkItemCount();

        internal static ThreadInt64PersistentCounter.ThreadLocalNode GetOrCreateThreadLocalCompletionCountNode() =>
            ThreadPool.UseWindowsThreadPool ?
            WindowsThreadPool.GetOrCreateThreadLocalCompletionCountNode() :
            PortableThreadPool.ThreadPoolInstance.GetOrCreateThreadLocalCompletionCountNode();

        public static bool SetMaxThreads(int workerThreads, int completionPortThreads) =>
            ThreadPool.UseWindowsThreadPool ?
            WindowsThreadPool.SetMaxThreads(workerThreads, completionPortThreads) :
            PortableThreadPool.ThreadPoolInstance.SetMaxThreads(workerThreads, completionPortThreads);

        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                WindowsThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
            }
            else
            {
                PortableThreadPool.ThreadPoolInstance.GetMaxThreads(out workerThreads, out completionPortThreads);
            }
        }

        public static bool SetMinThreads(int workerThreads, int completionPortThreads) =>
            ThreadPool.UseWindowsThreadPool ?
            WindowsThreadPool.SetMinThreads(workerThreads, completionPortThreads) :
            PortableThreadPool.ThreadPoolInstance.SetMinThreads(workerThreads, completionPortThreads);

        public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                WindowsThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            }
            else
            {
                PortableThreadPool.ThreadPoolInstance.GetMinThreads(out workerThreads, out completionPortThreads);
            }
        }

        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                WindowsThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
            }
            else
            {
                PortableThreadPool.ThreadPoolInstance.GetAvailableThreads(out workerThreads, out completionPortThreads);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void NotifyWorkItemProgress()
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                WindowsThreadPool.NotifyWorkItemProgress();
            }
            else
            {
                PortableThreadPool.ThreadPoolInstance.NotifyWorkItemProgress();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool NotifyWorkItemComplete(ThreadInt64PersistentCounter.ThreadLocalNode threadLocalCompletionCountNode, int currentTimeMs) =>
            ThreadPool.UseWindowsThreadPool ?
            WindowsThreadPool.NotifyWorkItemComplete(threadLocalCompletionCountNode, currentTimeMs) :
            PortableThreadPool.ThreadPoolInstance.NotifyWorkItemComplete(threadLocalCompletionCountNode, currentTimeMs);

        internal static bool NotifyThreadBlocked() =>
            ThreadPool.UseWindowsThreadPool ?
            WindowsThreadPool.NotifyThreadBlocked() :
            PortableThreadPool.ThreadPoolInstance.NotifyThreadBlocked();

        internal static void NotifyThreadUnblocked()
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                WindowsThreadPool.NotifyThreadUnblocked();
            }
            else
            {
                PortableThreadPool.ThreadPoolInstance.NotifyThreadUnblocked();
            }
        }

        /// <summary>
        /// This method is called to notify the thread pool about pending work.
        /// It will start with an ordinary read to check if a request is already pending as we
        /// optimize for a case when queues already have items and this flag is already set.
        /// Make sure that the presence of the item that is being added to the queue is visible
        /// before calling this.
        /// Typically this is not a problem when enqueing uses an interlocked update of the queue
        /// index to establish the presence of the new item. More care may be needed when an item
        /// is inserted via ordinary or volatile writes.
        /// </summary>
        internal static void EnsureWorkerRequested()
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                WindowsThreadPool.EnsureWorkerRequested();
            }
            else
            {
                PortableThreadPool.ThreadPoolInstance.EnsureWorkerRequested();
            }
        }

        internal static void ReportThreadStatus(bool isWorking)
        {
            Debug.Assert(!ThreadPool.UseWindowsThreadPool);
            PortableThreadPool.ThreadPoolInstance.ReportThreadStatus(isWorking);
        }

        /// <summary>
        /// Gets the number of thread pool threads that currently exist.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of threads, the count includes all types.
        /// </remarks>
        public static int ThreadCount
        {
            get
            {
                return ThreadPool.UseWindowsThreadPool ? WindowsThreadPool.ThreadCount : PortableThreadPool.ThreadPoolInstance.ThreadCount;
            }
        }

        /// <summary>
        /// Gets the number of work items that have been processed so far.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of work items, the count includes all types.
        /// </remarks>
        public static long CompletedWorkItemCount
        {
            get
            {
                return ThreadPool.UseWindowsThreadPool ? WindowsThreadPool.CompletedWorkItemCount : PortableThreadPool.ThreadPoolInstance.CompletedWorkItemCount;
            }
        }

        private static RegisteredWaitHandle RegisterWaitForSingleObject(
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object? state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce,
             bool flowExecutionContext)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                return WindowsThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext);
            }
            else
            {
                return PortableThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext);
            }
        }
    }
}
