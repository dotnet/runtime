// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    [UnsupportedOSPlatform("browser")]
    public sealed class RegisteredWaitHandle : MarshalByRefObject
    {
        internal RegisteredWaitHandle()
        {
        }

        public bool Unregister(WaitHandle? waitObject)
        {
            throw new PlatformNotSupportedException();
        }
    }

    public static partial class ThreadPool
    {
        // Time-sensitive work items are those that may need to run ahead of normal work items at least periodically. For a
        // runtime that does not support time-sensitive work items on the managed side, the thread pool yields the thread to the
        // runtime periodically (by exiting the dispatch loop) so that the runtime may use that thread for processing
        // any time-sensitive work. For a runtime that supports time-sensitive work items on the managed side, the thread pool
        // does not yield the thread and instead processes time-sensitive work items queued by specific APIs periodically.
        internal const bool SupportsTimeSensitiveWorkItems = false; // the timer currently doesn't queue time-sensitive work

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

        internal static void RequestWorkerThread()
        {
            if (_callbackQueued)
                return;
            _callbackQueued = true;
            QueueCallback();
        }

        internal static void NotifyWorkItemProgress()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool NotifyWorkItemComplete(object? threadLocalCompletionCountObject, int currentTimeMs)
        {
            return true;
        }

        internal static bool NotifyThreadBlocked() => false;

        internal static void NotifyThreadUnblocked()
        {
        }

        internal static object? GetOrCreateThreadLocalCompletionCountObject() => null;

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

        [DynamicDependency("Callback")]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void QueueCallback();

        private static void Callback()
        {
            _callbackQueued = false;
            ThreadPoolWorkQueue.Dispatch();
        }

        private static unsafe void NativeOverlappedCallback(nint overlappedPtr) =>
            _IOCompletionCallback.PerformSingleIOCompletionCallback((NativeOverlapped*)overlappedPtr, 0, 0);

        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public static unsafe bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped)
        {
            if (overlapped == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.overlapped);
            }

            // OS doesn't signal handle, so do it here (CoreCLR does this assignment in ThreadPoolNative::CorPostQueuedCompletionStatus)
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
    }
}
