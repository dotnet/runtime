// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public static unsafe bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                return WindowsThreadPool.UnsafeQueueNativeOverlapped(overlapped);
            }
            else
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
        }

        [Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
        [SupportedOSPlatform("windows")]
        public static bool BindHandle(IntPtr osHandle)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                return WindowsThreadPool.BindHandle(osHandle);
            }
            else
            {
                PortableThreadPool.ThreadPoolInstance.RegisterForIOCompletionNotifications(osHandle);
                return true;
            }
        }

        [SupportedOSPlatform("windows")]
        public static bool BindHandle(SafeHandle osHandle)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                return WindowsThreadPool.BindHandle(osHandle);
            }
            else
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
        }

        internal static bool EnsureConfigInitialized() => EnsureConfigInitializedCore();

        internal static void InitializeForThreadPoolThread() => WindowsThreadPool.InitializeForThreadPoolThread();

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
                WindowsThreadPool.GetMinThreads(workerThreads, completionPortThreads);
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
                WindowsThreadPool.GetAvailableThreads(workerThreads, completionPortThreads);
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
    }
}
