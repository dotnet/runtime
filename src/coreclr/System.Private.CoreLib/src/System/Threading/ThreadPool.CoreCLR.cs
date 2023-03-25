// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
**
** Purpose: Class for creating and managing a threadpool
**
**
=============================================================================*/

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    public static partial class ThreadPool
    {
        internal static bool EnsureConfigInitialized() => EnsureConfigInitializedCore();

        public static bool SetMaxThreads(int workerThreads, int completionPortThreads)
        {
            return PortableThreadPool.ThreadPoolInstance.SetMaxThreads(workerThreads, completionPortThreads);
        }

        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
        {
            PortableThreadPool.ThreadPoolInstance.GetMaxThreads(out workerThreads, out completionPortThreads);
        }

        public static bool SetMinThreads(int workerThreads, int completionPortThreads)
        {
            return PortableThreadPool.ThreadPoolInstance.SetMinThreads(workerThreads, completionPortThreads);
        }

        public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
        {
            PortableThreadPool.ThreadPoolInstance.GetMinThreads(out workerThreads, out completionPortThreads);
        }

        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
        {
            PortableThreadPool.ThreadPoolInstance.GetAvailableThreads(out workerThreads, out completionPortThreads);
        }

        internal static void RequestWorkerThread()
        {
            PortableThreadPool.ThreadPoolInstance.RequestWorker();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool NotifyWorkItemComplete(object threadLocalCompletionCountObject, int currentTimeMs)
        {
            return
                PortableThreadPool.ThreadPoolInstance.NotifyWorkItemComplete(
                    threadLocalCompletionCountObject,
                    currentTimeMs);
        }

        internal static void ReportThreadStatus(bool isWorking)
        {
            PortableThreadPool.ThreadPoolInstance.ReportThreadStatus(isWorking);
        }

        internal static void NotifyWorkItemProgress()
        {
            PortableThreadPool.ThreadPoolInstance.NotifyWorkItemProgress();
        }

        internal static bool NotifyThreadBlocked() => PortableThreadPool.ThreadPoolInstance.NotifyThreadBlocked();

        internal static void NotifyThreadUnblocked()
        {
            PortableThreadPool.ThreadPoolInstance.NotifyThreadUnblocked();
        }

        internal static object? GetOrCreateThreadLocalCompletionCountObject() =>
            PortableThreadPool.ThreadPoolInstance.GetOrCreateThreadLocalCompletionCountObject();
    }
}
