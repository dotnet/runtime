// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    public static partial class ThreadPool
    {
        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        private static unsafe bool UnsafeQueueNativeOverlappedPortableCore(NativeOverlapped* overlapped) =>
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OverlappedIO);

        [Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
        [SupportedOSPlatform("windows")]
        private static bool BindHandlePortableCore(IntPtr osHandle) =>
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OverlappedIO);

        [SupportedOSPlatform("windows")]
        private static bool BindHandlePortableCore(SafeHandle osHandle) =>
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OverlappedIO);
    }
}
