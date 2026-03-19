// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        private unsafe PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData, bool flowExecutionContext)
        {
            ArgumentNullException.ThrowIfNull(callback);

            // Validate pinData blittability by packing (this throws ArgumentException for non-blittable types),
            // then immediately free since overlapped I/O is not supported on this platform.
            Overlapped overlapped = new Overlapped();
            NativeOverlapped* nativeOverlapped = flowExecutionContext
                ? overlapped.Pack(null, pinData)
                : overlapped.UnsafePack(null, pinData);
            Overlapped.Free(nativeOverlapped);
        }

        public void Dispose() { }
    }
}
