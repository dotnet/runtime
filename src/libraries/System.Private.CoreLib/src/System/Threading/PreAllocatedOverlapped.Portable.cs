// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    /// <summary>
    /// Represents pre-allocated state for native overlapped I/O operations.
    /// </summary>
    /// <seealso cref="ThreadPoolBoundHandle.AllocateNativeOverlapped(PreAllocatedOverlapped)"/>
    public sealed partial class PreAllocatedOverlapped : IDisposable, IDeferredDisposable
    {
        internal ThreadPoolBoundHandleOverlapped? _overlappedPortableCore;

        private static PreAllocatedOverlapped UnsafeCreatePortableCore(IOCompletionCallback callback, object? state, object? pinData) =>
            new PreAllocatedOverlapped(callback, state, pinData, flowExecutionContext: false);

        private bool AddRefPortableCore()
        {
            return _lifetime.AddRef();
        }

        private void ReleasePortableCore()
        {
            _lifetime.Release(this);
        }

        private void DisposePortableCore()
        {
            _lifetime.Dispose(this);
            GC.SuppressFinalize(this);
        }

        private unsafe void IDeferredDisposableOnFinalReleasePortableCore(bool disposed)
        {
            if (_overlappedPortableCore != null) // protect against ctor throwing exception and leaving field uninitialized
            {
                if (disposed)
                {
                    Overlapped.Free(_overlappedPortableCore._nativeOverlapped);
                }
                else
                {
                    _overlappedPortableCore._boundHandle = null;
                    _overlappedPortableCore._completed = false;
                    *_overlappedPortableCore._nativeOverlapped = default;
                }
            }
        }
    }
}
