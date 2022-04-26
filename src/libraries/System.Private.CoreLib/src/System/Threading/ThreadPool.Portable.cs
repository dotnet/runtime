// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    //
    // Portable implementation of ThreadPool
    //

    internal sealed partial class CompleteWaitThreadPoolWorkItem : IThreadPoolWorkItem
    {
        void IThreadPoolWorkItem.Execute() => PortableThreadPool.CompleteWait(_registeredWaitHandle, _timedOut);
    }

    public static partial class ThreadPool
    {
        internal static bool UsePortableThreadPoolForIO => true;

        // Indicates whether the thread pool should yield the thread from the dispatch loop to the runtime periodically so that
        // the runtime may use the thread for processing other work
        internal static bool YieldFromDispatchLoop => false;

#if CORERT
        private const bool IsWorkerTrackingEnabledInConfig = false;
#else
        private static readonly bool IsWorkerTrackingEnabledInConfig =
            AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.EnableWorkerTracking", false);
#endif

        // Threadpool specific initialization of a new thread. Used by OS-provided threadpools. No-op for portable threadpool.
        internal static void InitializeForThreadPoolThread() { }

#pragma warning disable IDE0060
        internal static bool CanSetMinIOCompletionThreads(int ioCompletionThreads) => false;
        internal static bool CanSetMaxIOCompletionThreads(int ioCompletionThreads) => false;
#pragma warning restore IDE0060

        [Conditional("unnecessary")]
        internal static void SetMinIOCompletionThreads(int ioCompletionThreads) { }
        [Conditional("unnecessary")]
        internal static void SetMaxIOCompletionThreads(int ioCompletionThreads) { }

        public static bool SetMaxThreads(int workerThreads, int completionPortThreads) =>
            PortableThreadPool.ThreadPoolInstance.SetMaxThreads(workerThreads, completionPortThreads);
        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads) =>
            PortableThreadPool.ThreadPoolInstance.GetMaxThreads(out workerThreads, out completionPortThreads);
        public static bool SetMinThreads(int workerThreads, int completionPortThreads) =>
            PortableThreadPool.ThreadPoolInstance.SetMinThreads(workerThreads, completionPortThreads);
        public static void GetMinThreads(out int workerThreads, out int completionPortThreads) =>
            PortableThreadPool.ThreadPoolInstance.GetMinThreads(out workerThreads, out completionPortThreads);
        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads) =>
            PortableThreadPool.ThreadPoolInstance.GetAvailableThreads(out workerThreads, out completionPortThreads);

        /// <summary>
        /// Gets the number of thread pool threads that currently exist.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of threads, the count includes all types.
        /// </remarks>
        public static int ThreadCount => PortableThreadPool.ThreadPoolInstance.ThreadCount;

        /// <summary>
        /// Gets the number of work items that have been processed by the thread pool so far.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of work items, the count includes all types.
        /// </remarks>
        public static long CompletedWorkItemCount => PortableThreadPool.ThreadPoolInstance.CompletedWorkItemCount;

        /// <summary>
        /// This method is called to request a new thread pool worker to handle pending work.
        /// </summary>
        internal static void RequestWorkerThread() => PortableThreadPool.ThreadPoolInstance.RequestWorker();

        /// <summary>
        /// Called from the gate thread periodically to perform runtime-specific gate activities
        /// </summary>
        /// <param name="cpuUtilization">CPU utilization as a percentage since the last call</param>
        /// <returns>True if the runtime still needs to perform gate activities, false otherwise</returns>
#pragma warning disable IDE0060
        internal static bool PerformRuntimeSpecificGateActivities(int cpuUtilization) => false;
#pragma warning restore IDE0060

        internal static void NotifyWorkItemProgress() => PortableThreadPool.ThreadPoolInstance.NotifyWorkItemProgress();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool NotifyWorkItemComplete(object? threadLocalCompletionCountObject, int currentTimeMs) =>
            PortableThreadPool.ThreadPoolInstance.NotifyWorkItemComplete(threadLocalCompletionCountObject, currentTimeMs);

        internal static bool NotifyThreadBlocked() => PortableThreadPool.ThreadPoolInstance.NotifyThreadBlocked();
        internal static void NotifyThreadUnblocked() => PortableThreadPool.ThreadPoolInstance.NotifyThreadUnblocked();

        internal static object GetOrCreateThreadLocalCompletionCountObject() =>
            PortableThreadPool.ThreadPoolInstance.GetOrCreateThreadLocalCompletionCountObject();

        private static RegisteredWaitHandle RegisterWaitForSingleObject(
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object? state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce,
             bool flowExecutionContext)
        {
            ArgumentNullException.ThrowIfNull(waitObject);
            ArgumentNullException.ThrowIfNull(callBack);

            RegisteredWaitHandle registeredHandle = new RegisteredWaitHandle(
                waitObject,
                new _ThreadPoolWaitOrTimerCallback(callBack, state, flowExecutionContext),
                (int)millisecondsTimeOutInterval,
                !executeOnlyOnce);
            PortableThreadPool.ThreadPoolInstance.RegisterWaitHandle(registeredHandle);
            return registeredHandle;
        }
    }
}
