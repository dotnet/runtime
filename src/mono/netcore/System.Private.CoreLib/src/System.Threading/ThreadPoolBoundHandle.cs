// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed class ThreadPoolBoundHandle : IDisposable
    {
        public SafeHandle Handle => null;

        private ThreadPoolBoundHandle() { }

        public static ThreadPoolBoundHandle BindHandle(SafeHandle handle)
        {
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));

            if (handle.IsClosed || handle.IsInvalid)
                throw new ArgumentException(SR.Argument_InvalidHandle, nameof(handle));

            throw new PlatformNotSupportedException(SR.NotSupported_Overlapped);
        }

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* AllocateNativeOverlapped(IOCompletionCallback callback, object state, object pinData)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            throw new PlatformNotSupportedException(SR.NotSupported_Overlapped);
        }

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* AllocateNativeOverlapped(PreAllocatedOverlapped preAllocated)
        {
            if (preAllocated == null)
                throw new ArgumentNullException(nameof(preAllocated));

            throw new PlatformNotSupportedException(SR.NotSupported_Overlapped);
        }

        [CLSCompliant(false)]
        public unsafe void FreeNativeOverlapped(NativeOverlapped* overlapped)
        {
            if (overlapped == null)
                throw new ArgumentNullException(nameof(overlapped));

            throw new PlatformNotSupportedException(SR.NotSupported_Overlapped);
        }

        [CLSCompliant(false)]
        public static unsafe object GetNativeOverlappedState(NativeOverlapped* overlapped)
        {
            if (overlapped == null)
                throw new ArgumentNullException(nameof(overlapped));

            throw new PlatformNotSupportedException(SR.NotSupported_Overlapped);
        }

        public void Dispose() { }
    }
}