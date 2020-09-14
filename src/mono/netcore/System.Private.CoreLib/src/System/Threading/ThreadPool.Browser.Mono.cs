// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    [UnsupportedOSPlatform("browser")]
    public sealed class RegisteredWaitHandle : MarshalByRefObject
    {
        internal RegisteredWaitHandle(WaitHandle waitHandle, _ThreadPoolWaitOrTimerCallback callbackHelper,
            int millisecondsTimeout, bool repeating)
        {
        }

        public bool Unregister(WaitHandle? waitObject)
        {
            throw new PlatformNotSupportedException();
        }
    }

    public static partial class ThreadPool
    {
        internal const bool EnableWorkerTracking = false;

        private static bool _callbackQueued;

        internal static void InitializeForThreadPoolThread() { }

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

        internal static bool KeepDispatching(int startTickCount)
        {
            return true;
        }

        internal static void NotifyWorkItemProgress()
        {
        }

        internal static bool NotifyWorkItemComplete()
        {
            return true;
        }

        private static RegisteredWaitHandle RegisterWaitForSingleObject(
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object? state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce,
             bool flowExecutionContext)
        {
            if (waitObject == null)
                throw new ArgumentNullException(nameof(waitObject));

            if (callBack == null)
                throw new ArgumentNullException(nameof(callBack));

            throw new PlatformNotSupportedException();
        }

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
