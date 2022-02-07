// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    public sealed class PreAllocatedOverlapped : IDisposable
    {
        [CLSCompliant(false)]
        public PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData) :
            this(callback, state, pinData, flowExecutionContext: true)
        {
        }

        [CLSCompliant(false)]
        public static PreAllocatedOverlapped UnsafeCreate(IOCompletionCallback callback, object? state, object? pinData) =>
            new PreAllocatedOverlapped(callback, state, pinData, flowExecutionContext: false);

        private PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData, bool flowExecutionContext)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            throw new PlatformNotSupportedException(SR.PlatformNotSupported_OverlappedIO);
        }

        public void Dispose()
        {
        }
    }
}
