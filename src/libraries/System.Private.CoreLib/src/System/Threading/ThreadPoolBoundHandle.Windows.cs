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

        private static ThreadPoolBoundHandle BindHandlePortableCore(SafeHandle handle)
        {
            ArgumentNullException.ThrowIfNull(handle);

            if (handle.IsClosed || handle.IsInvalid)
                throw new ArgumentException(SR.Argument_InvalidHandle, nameof(handle));

            return BindHandleWindowsCore(handle);
        }

        private static ThreadPoolBoundHandle BindHandleWindowsCore(SafeHandle handle)
        {
            Debug.Assert(handle != null);
            Debug.Assert(!handle.IsClosed);
            Debug.Assert(!handle.IsInvalid);

            try
            {
                Debug.Assert(OperatingSystem.IsWindows());
                // ThreadPool.BindHandle will always return true, otherwise, it throws.
                bool succeeded = ThreadPool.BindHandle(handle);
                Debug.Assert(succeeded);
            }
            catch (Exception ex)
            {   // BindHandle throws ApplicationException on full CLR and Exception on CoreCLR.
                // We do not let either of these leak and convert them to ArgumentException to
                // indicate that the specified handles are invalid.

                if (ex.HResult == HResults.E_HANDLE)         // Bad handle
                    throw new ArgumentException(SR.Argument_InvalidHandle, nameof(handle));

                if (ex.HResult == HResults.E_INVALIDARG)     // Handle already bound or sync handle
                    throw new ArgumentException(SR.Argument_AlreadyBoundOrSyncHandle, nameof(handle));

                throw;
            }

            return new ThreadPoolBoundHandle(handle);
        }

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
            if (ThreadPool.UseWindowsThreadPool)
            {
                FinalizeWindowsThreadPool();
            }
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
