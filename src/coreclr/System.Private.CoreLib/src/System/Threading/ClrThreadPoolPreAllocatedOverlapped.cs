// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    /// <summary>
    /// Represents pre-allocated state for native overlapped I/O operations.
    /// </summary>
    /// <seealso cref="ThreadPoolBoundHandle.AllocateNativeOverlapped(PreAllocatedOverlapped)"/>
    public sealed class PreAllocatedOverlapped : IDisposable, IDeferredDisposable
    {
        internal readonly ThreadPoolBoundHandleOverlapped _overlapped;
        private DeferredDisposableLifetime<PreAllocatedOverlapped> _lifetime;

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
        ///     instance can be reused for subsequent native operations.
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
        [CLSCompliant(false)]
        public PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            _overlapped = new ThreadPoolBoundHandleOverlapped(callback, state, pinData, this);
        }

        internal bool AddRef()
        {
            return _lifetime.AddRef();
        }

        internal void Release()
        {
            _lifetime.Release(this);
        }

        /// <summary>
        /// Frees the resources associated with this <see cref="PreAllocatedOverlapped"/> instance.
        /// </summary>
        public void Dispose()
        {
            _lifetime.Dispose(this);
            GC.SuppressFinalize(this);
        }

        ~PreAllocatedOverlapped()
        {
            Dispose();
        }

        unsafe void IDeferredDisposable.OnFinalRelease(bool disposed)
        {
            if (_overlapped != null) // protect against ctor throwing exception and leaving field uninitialized
            {
                if (disposed)
                {
                    Overlapped.Free(_overlapped._nativeOverlapped);
                }
                else
                {
                    _overlapped._boundHandle = null;
                    _overlapped._completed = false;
                    *_overlapped._nativeOverlapped = default;
                }
            }
        }

        internal bool IsUserObject(byte[]? buffer) => _overlapped.IsUserObject(buffer);
    }
}
