// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public sealed partial class Thread
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ResetThreadPoolThread()
        {
            Debug.Assert(this == CurrentThread);
            Debug.Assert(IsThreadPoolThread);

            if (_mayNeedResetForThreadPool)
            {
                ResetThreadPoolThreadSlow();
            }
        }
    }

    public sealed partial class RegisteredWaitHandle : MarshalByRefObject
    {
        public bool Unregister(WaitHandle? waitObject)
        {
            throw new PlatformNotSupportedException();
        }
    }

    internal sealed partial class CompleteWaitThreadPoolWorkItem : IThreadPoolWorkItem
    {
        void IThreadPoolWorkItem.Execute()
        {
            Debug.Fail("Registered wait handles are currently not supported");
        }
    }

    public static partial class ThreadPool
    {
        internal const bool SupportsTimeSensitiveWorkItems = true;
        internal const bool EnableWorkerTracking = false;

        private static bool _callbackQueued;

        internal static bool CanSetMinIOCompletionThreads(int ioCompletionThreads) => true;
        internal static void SetMinIOCompletionThreads(int ioCompletionThreads) { }

        internal static bool CanSetMaxIOCompletionThreads(int ioCompletionThreads) => true;
        internal static void SetMaxIOCompletionThreads(int ioCompletionThreads) { }

        public static bool SetMaxThreads(int workerThreads, int completionPortThreads)
        {
            if (workerThreads == 1 && completionPortThreads == 1)
                return true;
            return false;
        }

        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
        {
            workerThreads = 1;
            completionPortThreads = 1;
        }

        public static bool SetMinThreads(int workerThreads, int completionPortThreads)
        {
            if (workerThreads == 1 && completionPortThreads == 1)
                return true;
            return false;
        }

        public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
        {
            workerThreads = 1;
            completionPortThreads = 1;
        }

        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
        {
            workerThreads = 1;
            completionPortThreads = 1;
        }

        public static int ThreadCount => 1;

        public static long CompletedWorkItemCount => 0;

        internal static void RequestWorkerThread()
        {
            if (_callbackQueued)
                return;
            _callbackQueued = true;
            QueueCallback();
        }


        /// <summary>
        /// Called from the gate thread periodically to perform runtime-specific gate activities
        /// </summary>
        /// <param name="cpuUtilization">CPU utilization as a percentage since the last call</param>
        /// <returns>True if the runtime still needs to perform gate activities, false otherwise</returns>
        internal static bool PerformRuntimeSpecificGateActivities(int cpuUtilization) => false;

        internal static void NotifyWorkItemProgress()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool NotifyWorkItemComplete(object? threadLocalCompletionCountObject, int currentTimeMs)
        {
            return true;
        }

        internal static object? GetOrCreateThreadLocalCompletionCountObject() => null;

        private static void RegisterWaitForSingleObjectCore(WaitHandle? waitObject, RegisteredWaitHandle registeredWaitHandle) =>
            throw new PlatformNotSupportedException();

        internal static void UnsafeQueueWaitCompletion(CompleteWaitThreadPoolWorkItem completeWaitWorkItem) =>
            UnsafeQueueUserWorkItemInternal(completeWaitWorkItem, preferLocal: false);

        [DynamicDependency("Callback")]
        [DynamicDependency("PumpThreadPool")]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void QueueCallback();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void PumpThreadPool(); // NOTE: this method is called via reflection by test code

        private static void Callback()
        {
            _callbackQueued = false;
            ThreadPoolWorkQueue.Dispatch();
        }
    }
}
