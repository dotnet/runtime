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
