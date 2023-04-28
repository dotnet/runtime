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
        internal static readonly bool UseWindowsThreadPool =
            Environment.GetEnvironmentVariable("DOTNET_ThreadPool_UseWindowsThreadPool") == "1" ||
            AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.UseWindowsThreadPool", false);

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

        internal static bool EnsureConfigInitialized() => EnsureConfigInitializedCore();

        internal static void InitializeForThreadPoolThread()
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                WindowsThreadPool.InitializeForThreadPoolThread();
            }
            else
            {
                InitializeForThreadPoolThreadPortableCore();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void IncrementCompletedWorkItemCount() => WindowsThreadPool.IncrementCompletedWorkItemCount();

        internal static object GetOrCreateThreadLocalCompletionCountObject() =>
            ThreadPool.UseWindowsThreadPool ?
            WindowsThreadPool.GetOrCreateThreadLocalCompletionCountObject() :
            GetOrCreateThreadLocalCompletionCountObjectPortableCore();

        public static bool SetMaxThreads(int workerThreads, int completionPortThreads) =>
            ThreadPool.UseWindowsThreadPool ?
            WindowsThreadPool.SetMaxThreads(workerThreads, completionPortThreads) :
            SetMaxThreadsPortableCore(workerThreads, completionPortThreads);

        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                WindowsThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
            }
            else
            {
                GetMaxThreadsPortableCore(out workerThreads, out completionPortThreads);
            }
        }

        public static bool SetMinThreads(int workerThreads, int completionPortThreads) =>
            SetMinThreadsPortableCore(workerThreads, completionPortThreads);

        public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                WindowsThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            }
            else
            {
                GetMinThreadsPortableCore(out workerThreads, out completionPortThreads);
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
                GetAvailableThreadsPortableCore(out workerThreads, out completionPortThreads);
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
                NotifyWorkItemProgressPortableCore();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool NotifyWorkItemComplete(object threadLocalCompletionCountObject, int currentTimeMs) =>
            ThreadPool.UseWindowsThreadPool ?
            WindowsThreadPool.NotifyWorkItemComplete(threadLocalCompletionCountObject, currentTimeMs) :
            NotifyWorkItemCompletePortableCore(threadLocalCompletionCountObject, currentTimeMs);

        internal static bool NotifyThreadBlocked() =>
            ThreadPool.UseWindowsThreadPool ?
            WindowsThreadPool.NotifyThreadBlocked() :
            NotifyThreadBlockedPortableCore();

        internal static void NotifyThreadUnblocked()
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                WindowsThreadPool.NotifyThreadUnblocked();
            }
            else
            {
                NotifyThreadUnblockedPortableCore();
            }
        }

        internal static unsafe void RequestWorkerThread()
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                WindowsThreadPool.RequestWorkerThread();
            }
            else
            {
                RequestWorkerThreadPortableCore();
            }
        }

        internal static void ReportThreadStatus(bool isWorking)
        {
            Debug.Assert(!ThreadPool.UseWindowsThreadPool);
            ReportThreadStatusCore(isWorking);
        }
    }
}
