// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    /// <summary>
    /// Represents pre-allocated state for native overlapped I/O operations.
    /// </summary>
    /// <seealso cref="ThreadPoolBoundHandle.AllocateNativeOverlapped(PreAllocatedOverlapped)"/>
    public sealed partial class PreAllocatedOverlapped : IDisposable, IDeferredDisposable
    {
        internal ThreadPoolBoundHandleOverlapped? _overlappedPortableCore;

        // Unbound PreAllocatedOverlapped instances are not associated with a ThreadPoolBoundHandle.
        // They are used in SafeWaitHandle.WaitCompletionPacket, because wait completion packets are
        // not bound to an I/O completion port permanently, but every time they are associated with
        // a wait handle.

        internal static PreAllocatedOverlapped UnsafeCreatePortableUnbound(IOCompletionCallback callback, object? state, object? pinData)
        {
            PreAllocatedOverlapped instance = UnsafeCreate(callback, state, pinData);
            instance._overlappedPortableCore!._isUnboundPreAllocatedOverlapped = true;
            return instance;
        }

        internal unsafe NativeOverlapped* AllocateNativeOverlappedPortableUnbound()
        {
            AddRef();
            Debug.Assert(_overlappedPortableCore!._boundHandle is null);
            return _overlappedPortableCore._nativeOverlapped;
        }

        internal unsafe void FreeNativeOverlappedPortableUnbound(NativeOverlapped* overlapped)
        {
            ArgumentNullException.ThrowIfNull(overlapped);

            Debug.Assert(_overlappedPortableCore!._boundHandle is null);
            Release();
        }

        internal static unsafe object? GetNativeOverlappedStatePortableUnbound(NativeOverlapped* overlapped)
        {
            ArgumentNullException.ThrowIfNull(overlapped);

            ThreadPoolBoundHandleOverlapped wrapper = ThreadPoolBoundHandleOverlapped.GetOverlappedWrapper(overlapped);
            Debug.Assert(wrapper._boundHandle is null);
            return wrapper._userState;
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
