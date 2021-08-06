// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    public static partial class ThreadPool
    {
        internal static void ReportThreadStatus(bool isWorking)
        {
        }

        private static unsafe void NativeOverlappedCallback(object? obj)
        {
            NativeOverlapped* overlapped = (NativeOverlapped*)(IntPtr)obj!;
            _IOCompletionCallback.PerformIOCompletionCallback(0, 0, overlapped);
        }

        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public static unsafe bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped)
        {
            // OS doesn't signal handle, so do it here (CoreCLR does this assignment in ThreadPoolNative::CorPostQueuedCompletionStatus)
            overlapped->InternalLow = (IntPtr)0;
            // Both types of callbacks are executed on the same thread pool
            return UnsafeQueueUserWorkItem(NativeOverlappedCallback, (IntPtr)overlapped);
        }

        [Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.", false)]
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

        private static long PendingUnmanagedWorkItemCount => 0;
    }
}
