// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    internal sealed partial class CompleteWaitThreadPoolWorkItem : IThreadPoolWorkItem
    {
        void IThreadPoolWorkItem.Execute() => CompleteWait();

        // Entry point from unmanaged code
        private void CompleteWait()
        {
            PortableThreadPool.CompleteWait(_registeredWaitHandle, _timedOut);
        }
    }

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
            ReportThreadStatusCore(isWorking);
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
                return PortableThreadPool.ThreadPoolInstance.ThreadCount;
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
                return PortableThreadPool.ThreadPoolInstance.CompletedWorkItemCount;
            }
        }

        private static void InitializeForThreadPoolThreadPortableCore() { }

        [SupportedOSPlatform("windows")]
        private static unsafe bool UnsafeQueueNativeOverlappedPortableCore(NativeOverlapped* overlapped)
        {
            if (overlapped == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.overlapped);
            }

            // OS doesn't signal handle, so do it here
            overlapped->InternalLow = IntPtr.Zero;

            PortableThreadPool.ThreadPoolInstance.QueueNativeOverlapped(overlapped);
            return true;
        }

        [Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
        [SupportedOSPlatform("windows")]
        private static bool BindHandlePortableCore(IntPtr osHandle)
        {
            PortableThreadPool.ThreadPoolInstance.RegisterForIOCompletionNotifications(osHandle);
            return true;
        }

        [SupportedOSPlatform("windows")]
        private static bool BindHandlePortableCore(SafeHandle osHandle)
        {
            ArgumentNullException.ThrowIfNull(osHandle);

            bool mustReleaseSafeHandle = false;
            try
            {
                osHandle.DangerousAddRef(ref mustReleaseSafeHandle);

                PortableThreadPool.ThreadPoolInstance.RegisterForIOCompletionNotifications(osHandle.DangerousGetHandle());
                return true;
            }
            finally
            {
                if (mustReleaseSafeHandle)
                    osHandle.DangerousRelease();
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
            ArgumentNullException.ThrowIfNull(waitObject);
            ArgumentNullException.ThrowIfNull(callBack);

            RegisteredWaitHandle registeredWaitHandle = new RegisteredWaitHandle(
                waitObject,
                new _ThreadPoolWaitOrTimerCallback(callBack, state, flowExecutionContext),
                (int)millisecondsTimeOutInterval,
                !executeOnlyOnce);

            PortableThreadPool.ThreadPoolInstance.RegisterWaitHandle(registeredWaitHandle);

            return registeredWaitHandle;
        }

        private static void ReportThreadStatusCore(bool isWorking)
        {
            PortableThreadPool.ThreadPoolInstance.ReportThreadStatus(isWorking);
        }
    }
}
