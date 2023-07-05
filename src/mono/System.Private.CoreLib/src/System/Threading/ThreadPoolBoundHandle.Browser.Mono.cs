// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed class ThreadPoolBoundHandle : IDisposable
    {
        public SafeHandle Handle => null!;

        private ThreadPoolBoundHandle()
        {
        }

        public static ThreadPoolBoundHandle BindHandle(SafeHandle handle)
        {
            ArgumentNullException.ThrowIfNull(handle);

            if (handle.IsClosed || handle.IsInvalid)
                throw new ArgumentException(SR.Argument_InvalidHandle, nameof(handle));

            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OverlappedIO);
        }

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* AllocateNativeOverlapped(IOCompletionCallback callback, object? state, object? pinData)
        {
            ArgumentNullException.ThrowIfNull(callback);

            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OverlappedIO);
        }

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* UnsafeAllocateNativeOverlapped(IOCompletionCallback callback, object? state, object? pinData) =>
            AllocateNativeOverlapped(callback, state, pinData);

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* AllocateNativeOverlapped(PreAllocatedOverlapped preAllocated)
        {
            ArgumentNullException.ThrowIfNull(preAllocated);

            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OverlappedIO);
        }

        [CLSCompliant(false)]
        public unsafe void FreeNativeOverlapped(NativeOverlapped* overlapped)
        {
            ArgumentNullException.ThrowIfNull(overlapped);

            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OverlappedIO);
        }

        [CLSCompliant(false)]
        public static unsafe object? GetNativeOverlappedState(NativeOverlapped* overlapped)
        {
            ArgumentNullException.ThrowIfNull(overlapped);

            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OverlappedIO);
        }

        public void Dispose()
        {
        }
    }
}
