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

            // OS doesn't signal handle, so do it here
            overlapped->InternalLow = IntPtr.Zero;

            PortableThreadPool.ThreadPoolInstance.QueueNativeOverlapped(overlapped);
            return true;
        }

        [Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
        [SupportedOSPlatform("windows")]
        public static bool BindHandle(IntPtr osHandle)
        {
            PortableThreadPool.ThreadPoolInstance.RegisterForIOCompletionNotifications(osHandle);
            return true;
        }

        [SupportedOSPlatform("windows")]
        public static bool BindHandle(SafeHandle osHandle)
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
}
