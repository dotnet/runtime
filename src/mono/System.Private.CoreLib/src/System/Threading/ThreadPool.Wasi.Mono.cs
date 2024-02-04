// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

#pragma warning disable IDE0060

namespace System.Threading
{
#if FEATURE_WASM_MANAGED_THREADS
#error when compiled with FEATURE_WASM_MANAGED_THREADS
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
            throw new PlatformNotSupportedException();
        }

        internal static void NotifyWorkItemProgress()
        {
        }

        internal static bool NotifyThreadBlocked() => false;

        internal static void NotifyThreadUnblocked()
        {
            throw new PlatformNotSupportedException();
        }

        internal static object? GetOrCreateThreadLocalCompletionCountObject() => null;

        internal static bool NotifyWorkItemComplete(object? _1, int _2)
        {
            throw new PlatformNotSupportedException();
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

        private static unsafe void NativeOverlappedCallback(nint overlappedPtr) =>
            IOCompletionCallbackHelper.PerformSingleIOCompletionCallback(0, 0, (NativeOverlapped*)overlappedPtr);

        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public static unsafe bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped)
        {
            throw new PlatformNotSupportedException();
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
