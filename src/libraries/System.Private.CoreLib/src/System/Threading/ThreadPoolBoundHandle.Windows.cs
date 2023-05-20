// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    /// <summary>
    ///     Represents an I/O handle that is bound to the system thread pool and enables low-level
    ///     components to receive notifications for asynchronous I/O operations.
    /// </summary>
    public sealed partial class ThreadPoolBoundHandle : IDisposable, IDeferredDisposable
    {
        private readonly SafeHandle _handle;
        private readonly SafeThreadPoolIOHandle? _threadPoolHandle;
        private DeferredDisposableLifetime<ThreadPoolBoundHandle> _lifetime;
        private bool _isDisposed;

        private ThreadPoolBoundHandle(SafeHandle handle, SafeThreadPoolIOHandle threadPoolHandle)
        {
            _threadPoolHandle = threadPoolHandle;
            _handle = handle;
        }

        private ThreadPoolBoundHandle(SafeHandle handle)
        {
            Debug.Assert(!ThreadPool.UseWindowsThreadPool);

            _handle = handle;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Gets the bound operating system handle.
        /// </summary>
        /// <value>
        ///     A <see cref="SafeHandle"/> object that holds the bound operating system handle.
        /// </value>
        public SafeHandle Handle => _handle;

        /// <summary>
        ///     Returns a <see cref="ThreadPoolBoundHandle"/> for the specific handle,
        ///     which is bound to the system thread pool.
        /// </summary>
        /// <param name="handle">
        ///     A <see cref="SafeHandle"/> object that holds the operating system handle. The
        ///     handle must have been opened for overlapped I/O on the unmanaged side.
        /// </param>
        /// <returns>
        ///     <see cref="ThreadPoolBoundHandle"/> for <paramref name="handle"/>, which
        ///     is bound to the system thread pool.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="handle"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="handle"/> has been disposed.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="handle"/> does not refer to a valid I/O handle.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="handle"/> refers to a handle that has not been opened
        ///     for overlapped I/O.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="handle"/> refers to a handle that has already been bound.
        /// </exception>
        /// <remarks>
        ///     This method should be called once per handle.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <see cref="ThreadPoolBoundHandle"/> does not take ownership of <paramref name="handle"/>,
        ///     it remains the responsibility of the caller to call <see cref="SafeHandle.Dispose()"/>.
        /// </remarks>
        public static unsafe ThreadPoolBoundHandle BindHandle(SafeHandle handle) =>
            ThreadPool.UseWindowsThreadPool ? BindHandleWindowsThreadPool(handle) : BindHandlePortableCore(handle);

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* AllocateNativeOverlapped(IOCompletionCallback callback, object? state, object? pinData) =>
            ThreadPool.UseWindowsThreadPool ?
            AllocateNativeOverlappedWindowsThreadPool(callback, state, pinData) :
            AllocateNativeOverlappedPortableCore(callback, state, pinData);

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* UnsafeAllocateNativeOverlapped(IOCompletionCallback callback, object? state, object? pinData) =>
            ThreadPool.UseWindowsThreadPool ?
            UnsafeAllocateNativeOverlappedWindowsThreadPool(callback, state, pinData) :
            UnsafeAllocateNativeOverlappedPortableCore(callback, state, pinData);

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* AllocateNativeOverlapped(PreAllocatedOverlapped preAllocated) =>
            ThreadPool.UseWindowsThreadPool ?
            AllocateNativeOverlappedWindowsThreadPool(preAllocated) :
            AllocateNativeOverlappedPortableCore(preAllocated);

        [CLSCompliant(false)]
        public unsafe void FreeNativeOverlapped(NativeOverlapped* overlapped)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                FreeNativeOverlappedWindowsThreadPool(overlapped);
            }
            else
            {
                FreeNativeOverlappedPortableCore(overlapped);
            }
        }

        [CLSCompliant(false)]
        public static unsafe object? GetNativeOverlappedState(NativeOverlapped* overlapped) =>
            ThreadPool.UseWindowsThreadPool ?
            GetNativeOverlappedStateWindowsThreadPool(overlapped) :
            GetNativeOverlappedStatePortableCore(overlapped);

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

        ~ThreadPoolBoundHandle()
        {
            Debug.Assert(ThreadPool.UseWindowsThreadPool);
            FinalizeWindowsThreadPool();
        }

        void IDeferredDisposable.OnFinalRelease(bool disposed)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                IDeferredDisposableOnFinalReleaseWindowsThreadPool(disposed);
            }
        }
    }
}
