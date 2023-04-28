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
        // Indicates whether the thread pool should yield the thread from the dispatch loop to the runtime periodically so that
        // the runtime may use the thread for processing other work
        internal static bool YieldFromDispatchLoop => false;

#if NATIVEAOT
        private const bool IsWorkerTrackingEnabledInConfig = false;
#else
        private static readonly bool IsWorkerTrackingEnabledInConfig =
            AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.EnableWorkerTracking", false);
#endif


        private static bool EnsureConfigInitializedCore() {
            throw new NotImplementedException();
        }

        // Threadpool specific initialization of a new thread. Used by OS-provided threadpools. No-op for portable threadpool.
        private static void InitializeForThreadPoolThreadPortableCore() { }

#pragma warning disable IDE0060
        internal static bool CanSetMinIOCompletionThreads(int ioCompletionThreads) => false;
        internal static bool CanSetMaxIOCompletionThreads(int ioCompletionThreads) => false;
#pragma warning restore IDE0060

        [Conditional("unnecessary")]
        internal static void SetMinIOCompletionThreads(int ioCompletionThreads) { }
        [Conditional("unnecessary")]
        internal static void SetMaxIOCompletionThreads(int ioCompletionThreads) { }

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

            Thread.ThrowIfNoThreadStart();
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
