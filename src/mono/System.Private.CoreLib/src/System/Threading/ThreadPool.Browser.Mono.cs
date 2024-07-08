// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

#pragma warning disable IDE0060

namespace System.Threading
{
#if FEATURE_WASM_MANAGED_THREADS
#error when compiled with FEATURE_WASM_MANAGED_THREADS, we use PortableThreadPool.WorkerThread.Browser.Threads.Mono.cs
#endif
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
    public sealed class RegisteredWaitHandle : MarshalByRefObject
    {
        internal RegisteredWaitHandle()
        {
        }

#pragma warning disable CA1822 // Mark members as static
        internal bool Repeating => false;
#pragma warning restore CA1822

        public bool Unregister(WaitHandle? waitObject)
        {
            throw new PlatformNotSupportedException();
        }
    }

    public static partial class ThreadPool
    {
        // Indicates whether the thread pool should yield the thread from the dispatch loop to the runtime periodically so that
        // the runtime may use the thread for processing other work
        internal static bool YieldFromDispatchLoop => true;

        private const bool IsWorkerTrackingEnabledInConfig = false;

        private static bool _callbackQueued;

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

        internal static unsafe void RequestWorkerThread()
        {
            if (_callbackQueued)
                return;
            _callbackQueued = true;
            MainThreadScheduleBackgroundJob((void*)(delegate* unmanaged[Cdecl]<void>)&BackgroundJobHandler);
        }

        internal static void NotifyWorkItemProgress()
        {
        }

        internal static bool NotifyThreadBlocked() => false;

        internal static void NotifyThreadUnblocked()
        {
        }

        internal static object? GetOrCreateThreadLocalCompletionCountObject() => null;

        internal static bool NotifyWorkItemComplete(object? threadLocalCompletionCountObject, int currentTimeMs)
        {
            return true;
        }

        private static RegisteredWaitHandle RegisterWaitForSingleObject(
             WaitHandle? waitObject,
             WaitOrTimerCallback? callBack,
             object? state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce,
             bool flowExecutionContext)
        {
            throw new PlatformNotSupportedException();
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe void MainThreadScheduleBackgroundJob(void* callback);

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
#pragma warning restore CS3016
        // this callback will arrive on the bound thread, called from mono_background_exec
        private static void BackgroundJobHandler()
        {
            try
            {
                _callbackQueued = false;
                ThreadPoolWorkQueue.Dispatch();
            }
            catch (Exception e)
            {
                Environment.FailFast("ThreadPool.BackgroundJobHandler failed", e);
            }
        }

        private static unsafe void NativeOverlappedCallback(nint overlappedPtr) =>
            IOCompletionCallbackHelper.PerformSingleIOCompletionCallback(0, 0, (NativeOverlapped*)overlappedPtr);

        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public static unsafe bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped)
        {
            if (overlapped == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.overlapped);
            }

            // OS doesn't signal handle, so do it here
            overlapped->InternalLow = (IntPtr)0;
            // Both types of callbacks are executed on the same thread pool
            return UnsafeQueueUserWorkItem(NativeOverlappedCallback, (nint)overlapped, preferLocal: false);
        }

        [Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
        [SupportedOSPlatform("windows")]
        public static bool BindHandle(IntPtr osHandle)
        {
            throw new PlatformNotSupportedException(SR.Arg_PlatformNotSupported); // Replaced by ThreadPoolBoundHandle.BindHandle
        }

        [SupportedOSPlatform("windows")]
        public static bool BindHandle(SafeHandle osHandle)
        {
            throw new PlatformNotSupportedException(SR.Arg_PlatformNotSupported); // Replaced by ThreadPoolBoundHandle.BindHandle
        }

        [Conditional("unnecessary")]
        internal static void ReportThreadStatus(bool isWorking)
        {

        }
    }
}
