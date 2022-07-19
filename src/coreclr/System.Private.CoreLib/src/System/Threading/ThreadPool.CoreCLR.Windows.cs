// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    public static partial class ThreadPool
    {
        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public static unsafe bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped)
        {
            if (overlapped == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.overlapped);
            }

            if (UsePortableThreadPoolForIO)
            {
                // OS doesn't signal handle, so do it here
                overlapped->InternalLow = IntPtr.Zero;

                PortableThreadPool.ThreadPoolInstance.QueueNativeOverlapped(overlapped);
                return true;
            }

            return PostQueuedCompletionStatus(overlapped);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe bool PostQueuedCompletionStatus(NativeOverlapped* overlapped);

        [Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
        [SupportedOSPlatform("windows")]
        public static bool BindHandle(IntPtr osHandle)
        {
            if (UsePortableThreadPoolForIO)
            {
                PortableThreadPool.ThreadPoolInstance.RegisterForIOCompletionNotifications(osHandle);
                return true;
            }

            return BindIOCompletionCallbackNative(osHandle);
        }

        [SupportedOSPlatform("windows")]
        public static bool BindHandle(SafeHandle osHandle)
        {
            ArgumentNullException.ThrowIfNull(osHandle);

            bool mustReleaseSafeHandle = false;
            try
            {
                osHandle.DangerousAddRef(ref mustReleaseSafeHandle);

                if (UsePortableThreadPoolForIO)
                {
                    PortableThreadPool.ThreadPoolInstance.RegisterForIOCompletionNotifications(osHandle.DangerousGetHandle());
                    return true;
                }

                return BindIOCompletionCallbackNative(osHandle.DangerousGetHandle());
            }
            finally
            {
                if (mustReleaseSafeHandle)
                    osHandle.DangerousRelease();
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool BindIOCompletionCallbackNative(IntPtr fileHandle);
    }
}
