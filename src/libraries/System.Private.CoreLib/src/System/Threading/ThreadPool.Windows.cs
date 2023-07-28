// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    public static partial class ThreadPool
    {
        internal static bool UseWindowsThreadPool { get; } =
            AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.UseWindowsThreadPool", "DOTNET_ThreadPool_UseWindowsThreadPool");

#if NATIVEAOT
        private const bool IsWorkerTrackingEnabledInConfig = false;
#else
        private static readonly bool IsWorkerTrackingEnabledInConfig =
            UseWindowsThreadPool ? false : AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.EnableWorkerTracking", "DOTNET_ThreadPool_EnableWorkerTracking");
#endif

        // Indicates whether the thread pool should yield the thread from the dispatch loop to the runtime periodically so that
        // the runtime may use the thread for processing other work.
        //
        // Windows thread pool threads need to yield back to the thread pool periodically, otherwise those threads may be
        // considered to be doing long-running work and change thread pool heuristics, such as slowing or halting thread
        // injection.
        internal static bool YieldFromDispatchLoop => UseWindowsThreadPool;

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

#if !CORECLR
        internal static bool EnsureConfigInitialized() => true;
#endif

        internal static void InitializeForThreadPoolThread()
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                WindowsThreadPool.InitializeForThreadPoolThread();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void IncrementCompletedWorkItemCount() => WindowsThreadPool.IncrementCompletedWorkItemCount();

        internal static object GetOrCreateThreadLocalCompletionCountObject() =>
            ThreadPool.UseWindowsThreadPool ?
            WindowsThreadPool.GetOrCreateThreadLocalCompletionCountObject() :
            PortableThreadPool.ThreadPoolInstance.GetOrCreateThreadLocalCompletionCountObject();

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
        internal static bool NotifyWorkItemComplete(object threadLocalCompletionCountObject, int currentTimeMs) =>
            ThreadPool.UseWindowsThreadPool ?
            WindowsThreadPool.NotifyWorkItemComplete(threadLocalCompletionCountObject, currentTimeMs) :
            PortableThreadPool.ThreadPoolInstance.NotifyWorkItemComplete(threadLocalCompletionCountObject, currentTimeMs);

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
        /// This method is called to request a new thread pool worker to handle pending work.
        /// </summary>
        internal static unsafe void RequestWorkerThread()
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                WindowsThreadPool.RequestWorkerThread();
            }
            else
            {
                PortableThreadPool.ThreadPoolInstance.RequestWorker();
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
