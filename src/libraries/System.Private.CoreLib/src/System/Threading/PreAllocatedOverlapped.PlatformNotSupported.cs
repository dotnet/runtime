// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    public sealed class PreAllocatedOverlapped : IDisposable
    {
        [CLSCompliant(false)]
        public PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData)
        {
            ArgumentNullException.ThrowIfNull(callback);
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OverlappedIO);
        }

        [CLSCompliant(false)]
        public static PreAllocatedOverlapped UnsafeCreate(IOCompletionCallback callback, object? state, object? pinData)
        {
            ArgumentNullException.ThrowIfNull(callback);
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OverlappedIO);
        }

        public void Dispose() { }
    }
}
