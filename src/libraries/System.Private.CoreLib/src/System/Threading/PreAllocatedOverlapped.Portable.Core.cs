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
        internal readonly ThreadPoolBoundHandleOverlapped? _overlappedPortableCore;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PreAllocatedOverlapped"/> class, specifying
        ///     a delegate that is invoked when each asynchronous I/O operation is complete, a user-provided
        ///     object providing context, and managed objects that serve as buffers.
        /// </summary>
        /// <param name="callback">
        ///     An <see cref="IOCompletionCallback"/> delegate that represents the callback method
        ///     invoked when each asynchronous I/O operation completes.
        /// </param>
        /// <param name="state">
        ///     A user-provided object that distinguishes <see cref="NativeOverlapped"/> instance produced from this
        ///     object from other <see cref="NativeOverlapped"/> instances. Can be <see langword="null"/>.
        /// </param>
        /// <param name="pinData">
        ///     An object or array of objects representing the input or output buffer for the operations. Each
        ///     object represents a buffer, for example an array of bytes.  Can be <see langword="null"/>.
        /// </param>
        /// <remarks>
        ///     The new <see cref="PreAllocatedOverlapped"/> instance can be passed to
        ///     <see cref="ThreadPoolBoundHandle.AllocateNativeOverlapped(PreAllocatedOverlapped)"/>, to produce
        ///     a <see cref="NativeOverlapped"/> instance that can be passed to the operating system in overlapped
        ///     I/O operations.  A single <see cref="PreAllocatedOverlapped"/> instance can only be used for
        ///     a single native I/O operation at a time.  However, the state stored in the <see cref="PreAllocatedOverlapped"/>
        ///     instance can be reused for subsequent native operations. ExecutionContext is not flowed to the invocation
        ///     of the callback.
        ///     <note>
        ///         The buffers specified in <paramref name="pinData"/> are pinned until <see cref="Dispose"/> is called.
        ///     </note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="callback"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     This method was called after the <see cref="ThreadPoolBoundHandle"/> was disposed.
        /// </exception>
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

        /// <summary>
        /// Frees the resources associated with this <see cref="PreAllocatedOverlapped"/> instance.
        /// </summary>
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
