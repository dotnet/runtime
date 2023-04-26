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
        internal readonly ThreadPoolBoundHandleOverlapped? _overlapped_portable_core;

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
            if (_overlapped_portable_core != null) // protect against ctor throwing exception and leaving field uninitialized
            {
                if (disposed)
                {
                    Overlapped.Free(_overlapped_portable_core._nativeOverlapped);
                }
                else
                {
                    _overlapped_portable_core._boundHandle = null;
                    _overlapped_portable_core._completed = false;
                    *_overlapped_portable_core._nativeOverlapped = default;
                }
            }
        }
    }
}
