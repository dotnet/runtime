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
            _handle = handle;
        }

        /// <summary>
        ///     Gets the bound operating system handle.
        /// </summary>
        /// <value>
        ///     A <see cref="SafeHandle"/> object that holds the bound operating system handle.
        /// </value>
        public SafeHandle Handle => _handle;

        public static unsafe ThreadPoolBoundHandle BindHandle(SafeHandle handle) =>
            ThreadPool.UseWindowsThreadPool ? BindHandleCore(handle) : BindHandlePortableCore(handle);

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* AllocateNativeOverlapped(IOCompletionCallback callback, object? state, object? pinData) =>
            ThreadPool.UseWindowsThreadPool ?
            AllocateNativeOverlappedCore(callback, state, pinData) :
            AllocateNativeOverlappedPortableCore(callback, state, pinData);

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* UnsafeAllocateNativeOverlapped(IOCompletionCallback callback, object? state, object? pinData) =>
            ThreadPool.UseWindowsThreadPool ?
            UnsafeAllocateNativeOverlappedCore(callback, state, pinData) :
            UnsafeAllocateNativeOverlappedPortableCore(callback, state, pinData);

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* AllocateNativeOverlapped(PreAllocatedOverlapped preAllocated) =>
            ThreadPool.UseWindowsThreadPool ?
            AllocateNativeOverlappedCore(preAllocated) :
            AllocateNativeOverlappedPortableCore(preAllocated);

        [CLSCompliant(false)]
        public unsafe void FreeNativeOverlapped(NativeOverlapped* overlapped)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                FreeNativeOverlappedCore(overlapped);
            }
            else
            {
                FreeNativeOverlappedPortableCore(overlapped);
            }
        }

        [CLSCompliant(false)]
        public static unsafe object? GetNativeOverlappedState(NativeOverlapped* overlapped) =>
            ThreadPool.UseWindowsThreadPool ?
            GetNativeOverlappedStateCore(overlapped) :
            GetNativeOverlappedStatePortableCore(overlapped);

        public void Dispose()
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                DisposeCore();
            }
            else
            {
                DisposePortableCore();
            }
        }

        ~ThreadPoolBoundHandle()
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                FinalizeCore();
            }
        }

        void IDeferredDisposable.OnFinalRelease(bool disposed)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                IDeferredDisposableOnFinalReleaseCore(disposed);
            }
        }
    }
}
