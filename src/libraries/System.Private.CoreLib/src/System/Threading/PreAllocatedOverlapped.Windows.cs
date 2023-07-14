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
        public PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData) :
            this(callback, state, pinData, flowExecutionContext: true)
        {
        }

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
        [CLSCompliant(false)]
        public static PreAllocatedOverlapped UnsafeCreate(IOCompletionCallback callback, object? state, object? pinData) =>
            ThreadPool.UseWindowsThreadPool ? UnsafeCreateWindowsThreadPool(callback, state, pinData) : UnsafeCreatePortableCore(callback, state, pinData);

        private unsafe PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData, bool flowExecutionContext)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                ArgumentNullException.ThrowIfNull(callback);

                _overlappedWindowsThreadPool = Win32ThreadPoolNativeOverlapped.Allocate(callback, state, pinData, this, flowExecutionContext);
            }
            else
            {
                // This construction is duplicated in PreAllocatedOverlapped.Unix.cs
                // It has to either be duplicated or remove the 'readonly' part of _overlappedPortableCore
                ArgumentNullException.ThrowIfNull(callback);

                _overlappedPortableCore = new ThreadPoolBoundHandleOverlapped(callback, state, pinData, this, flowExecutionContext);
            }
        }

        internal bool AddRef() => ThreadPool.UseWindowsThreadPool ? AddRefWindowsThreadPool() : AddRefPortableCore();

        internal void Release()
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                ReleaseWindowsThreadPool();
            }
            else
            {
                ReleasePortableCore();
            }
        }

        /// <summary>
        /// Frees the resources associated with this <see cref="PreAllocatedOverlapped"/> instance.
        /// </summary>
        public void Dispose()
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                DisposeWindowsThreadPool();
            }
            else
            {
                DisposePortableCore();
            }
        }

        ~PreAllocatedOverlapped()
        {
            Dispose();
        }

        unsafe void IDeferredDisposable.OnFinalRelease(bool disposed)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                IDeferredDisposableOnFinalReleaseWindowsThreadPool(disposed);
            }
            else
            {
                IDeferredDisposableOnFinalReleasePortableCore(disposed);
            }
        }
    }
}
