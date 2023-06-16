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

        private static ThreadPoolBoundHandle BindHandleCore(SafeHandle handle)
        {
            ArgumentNullException.ThrowIfNull(handle);

            if (handle.IsClosed || handle.IsInvalid)
                throw new ArgumentException(SR.Argument_InvalidHandle, nameof(handle));

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
            ThreadPool.UseWindowsThreadPool ? BindHandleWindowsThreadPool(handle) : BindHandleCore(handle);

        /// <summary>
        ///     Returns an unmanaged pointer to a <see cref="NativeOverlapped"/> structure, specifying
        ///     a delegate that is invoked when the asynchronous I/O operation is complete, a user-provided
        ///     object providing context, and managed objects that serve as buffers.
        /// </summary>
        /// <param name="callback">
        ///     An <see cref="IOCompletionCallback"/> delegate that represents the callback method
        ///     invoked when the asynchronous I/O operation completes.
        /// </param>
        /// <param name="state">
        ///     A user-provided object that distinguishes this <see cref="NativeOverlapped"/> from other
        ///     <see cref="NativeOverlapped"/> instances. Can be <see langword="null"/>.
        /// </param>
        /// <param name="pinData">
        ///     An object or array of objects representing the input or output buffer for the operation. Each
        ///     object represents a buffer, for example an array of bytes.  Can be <see langword="null"/>.
        /// </param>
        /// <returns>
        ///     An unmanaged pointer to a <see cref="NativeOverlapped"/> structure.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The unmanaged pointer returned by this method can be passed to the operating system in
        ///         overlapped I/O operations. The <see cref="NativeOverlapped"/> structure is fixed in
        ///         physical memory until <see cref="FreeNativeOverlapped(NativeOverlapped*)"/> is called.
        ///     </para>
        ///     <para>
        ///         The buffer or buffers specified in <paramref name="pinData"/> must be the same as those passed
        ///         to the unmanaged operating system function that performs the asynchronous I/O.
        ///     </para>
        ///     <note>
        ///         The buffers specified in <paramref name="pinData"/> are pinned for the duration of
        ///         the I/O operation.
        ///     </note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="callback"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     This method was called after the <see cref="ThreadPoolBoundHandle"/> was disposed.
        /// </exception>
        [CLSCompliant(false)]
        public unsafe NativeOverlapped* AllocateNativeOverlapped(IOCompletionCallback callback, object? state, object? pinData) =>
            ThreadPool.UseWindowsThreadPool ?
            AllocateNativeOverlappedWindowsThreadPool(callback, state, pinData) :
            AllocateNativeOverlappedPortableCore(callback, state, pinData);

        /// <summary>
        ///     Returns an unmanaged pointer to a <see cref="NativeOverlapped"/> structure, specifying
        ///     a delegate that is invoked when the asynchronous I/O operation is complete, a user-provided
        ///     object providing context, and managed objects that serve as buffers.
        /// </summary>
        /// <param name="callback">
        ///     An <see cref="IOCompletionCallback"/> delegate that represents the callback method
        ///     invoked when the asynchronous I/O operation completes.
        /// </param>
        /// <param name="state">
        ///     A user-provided object that distinguishes this <see cref="NativeOverlapped"/> from other
        ///     <see cref="NativeOverlapped"/> instances. Can be <see langword="null"/>.
        /// </param>
        /// <param name="pinData">
        ///     An object or array of objects representing the input or output buffer for the operation. Each
        ///     object represents a buffer, for example an array of bytes.  Can be <see langword="null"/>.
        /// </param>
        /// <returns>
        ///     An unmanaged pointer to a <see cref="NativeOverlapped"/> structure.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The unmanaged pointer returned by this method can be passed to the operating system in
        ///         overlapped I/O operations. The <see cref="NativeOverlapped"/> structure is fixed in
        ///         physical memory until <see cref="FreeNativeOverlapped(NativeOverlapped*)"/> is called.
        ///     </para>
        ///     <para>
        ///         The buffer or buffers specified in <paramref name="pinData"/> must be the same as those passed
        ///         to the unmanaged operating system function that performs the asynchronous I/O.
        ///     </para>
        ///     <para>
        ///         <see cref="ExecutionContext"/> is not flowed to the invocation of the callback.
        ///     </para>
        ///     <note>
        ///         The buffers specified in <paramref name="pinData"/> are pinned for the duration of
        ///         the I/O operation.
        ///     </note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="callback"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     This method was called after the <see cref="ThreadPoolBoundHandle"/> was disposed.
        /// </exception>
        [CLSCompliant(false)]
        public unsafe NativeOverlapped* UnsafeAllocateNativeOverlapped(IOCompletionCallback callback, object? state, object? pinData) =>
            ThreadPool.UseWindowsThreadPool ?
            UnsafeAllocateNativeOverlappedWindowsThreadPool(callback, state, pinData) :
            UnsafeAllocateNativeOverlappedPortableCore(callback, state, pinData);

        /// <summary>
        ///     Returns an unmanaged pointer to a <see cref="NativeOverlapped"/> structure, using the callback,
        ///     state, and buffers associated with the specified <see cref="PreAllocatedOverlapped"/> object.
        /// </summary>
        /// <param name="preAllocated">
        ///     A <see cref="PreAllocatedOverlapped"/> object from which to create the NativeOverlapped pointer.
        /// </param>
        /// <returns>
        ///     An unmanaged pointer to a <see cref="NativeOverlapped"/> structure.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The unmanaged pointer returned by this method can be passed to the operating system in
        ///         overlapped I/O operations. The <see cref="NativeOverlapped"/> structure is fixed in
        ///         physical memory until <see cref="FreeNativeOverlapped(NativeOverlapped*)"/> is called.
        ///     </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="preAllocated"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="preAllocated"/> is currently in use for another I/O operation.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     This method was called after the <see cref="ThreadPoolBoundHandle"/> was disposed, or
        ///     this method was called after <paramref name="preAllocated"/> was disposed.
        /// </exception>
        /// <seealso cref="PreAllocatedOverlapped"/>
        [CLSCompliant(false)]
        public unsafe NativeOverlapped* AllocateNativeOverlapped(PreAllocatedOverlapped preAllocated) =>
            ThreadPool.UseWindowsThreadPool ?
            AllocateNativeOverlappedWindowsThreadPool(preAllocated) :
            AllocateNativeOverlappedPortableCore(preAllocated);

        /// <summary>
        ///     Frees the unmanaged memory associated with a <see cref="NativeOverlapped"/> structure
        ///     allocated by the <see cref="AllocateNativeOverlapped"/> method.
        /// </summary>
        /// <param name="overlapped">
        ///     An unmanaged pointer to the <see cref="NativeOverlapped"/> structure to be freed.
        /// </param>
        /// <remarks>
        ///     <note type="caution">
        ///         You must call the <see cref="FreeNativeOverlapped(NativeOverlapped*)"/> method exactly once
        ///         on every <see cref="NativeOverlapped"/> unmanaged pointer allocated using the
        ///         <see cref="AllocateNativeOverlapped"/> method.
        ///         If you do not call the <see cref="FreeNativeOverlapped(NativeOverlapped*)"/> method, you will
        ///         leak memory. If you call the <see cref="FreeNativeOverlapped(NativeOverlapped*)"/> method more
        ///         than once on the same <see cref="NativeOverlapped"/> unmanaged pointer, memory will be corrupted.
        ///     </note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="overlapped"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     This method was called after the <see cref="ThreadPoolBoundHandle"/> was disposed.
        /// </exception>
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

        /// <summary>
        ///     Returns the user-provided object specified when the <see cref="NativeOverlapped"/> instance was
        ///     allocated using the <see cref="AllocateNativeOverlapped(IOCompletionCallback, object, object)"/>.
        /// </summary>
        /// <param name="overlapped">
        ///     An unmanaged pointer to the <see cref="NativeOverlapped"/> structure from which to return the
        ///     associated user-provided object.
        /// </param>
        /// <returns>
        ///     A user-provided object that distinguishes this <see cref="NativeOverlapped"/>
        ///     from other <see cref="NativeOverlapped"/> instances, otherwise, <see langword="null"/> if one was
        ///     not specified when the instance was allocated using <see cref="AllocateNativeOverlapped"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="overlapped"/> is <see langword="null"/>.
        /// </exception>
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
