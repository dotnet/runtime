// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    //
    // Implementation of ThreadPoolBoundHandle that sits on top of the CLR's ThreadPool and Overlapped infrastructure
    //

    /// <summary>
    ///     Represents an I/O handle that is bound to the system thread pool and enables low-level
    ///     components to receive notifications for asynchronous I/O operations.
    /// </summary>
    public sealed partial class ThreadPoolBoundHandle : IDisposable
    {
        private static ThreadPoolBoundHandle BindHandlePortableCore(SafeHandle handle)
        {
            ArgumentNullException.ThrowIfNull(handle);

            if (handle.IsClosed || handle.IsInvalid)
                throw new ArgumentException(SR.Argument_InvalidHandle, nameof(handle));

            return BindHandleCore(handle);
        }

        private unsafe NativeOverlapped* AllocateNativeOverlappedCore(IOCompletionCallback callback, object? state, object? pinData) =>
            AllocateNativeOverlapped(callback, state, pinData, flowExecutionContext: true);

        private unsafe NativeOverlapped* UnsafeAllocateNativeOverlappedCore(IOCompletionCallback callback, object? state, object? pinData) =>
            AllocateNativeOverlapped(callback, state, pinData, flowExecutionContext: false);

        private unsafe NativeOverlapped* AllocateNativeOverlapped(IOCompletionCallback callback, object? state, object? pinData, bool flowExecutionContext)
        {
            ArgumentNullException.ThrowIfNull(callback);
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            ThreadPoolBoundHandleOverlapped overlapped = new ThreadPoolBoundHandleOverlapped(callback, state, pinData, preAllocated: null, flowExecutionContext);
            overlapped._boundHandle = this;
            return overlapped._nativeOverlapped;
        }
        
        private unsafe NativeOverlapped* AllocateNativeOverlappedCore(PreAllocatedOverlapped preAllocated)
        {
            ArgumentNullException.ThrowIfNull(preAllocated);
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            preAllocated.AddRef();
            try
            {
                ThreadPoolBoundHandleOverlapped overlapped = preAllocated._overlapped;

                if (overlapped._boundHandle != null)
                    throw new ArgumentException(SR.Argument_PreAllocatedAlreadyAllocated, nameof(preAllocated));

                overlapped._boundHandle = this;

                return overlapped._nativeOverlapped;
            }
            catch
            {
                preAllocated.Release();
                throw;
            }
        }

        private unsafe void FreeNativeOverlappedCore(NativeOverlapped* overlapped)
        {
            ArgumentNullException.ThrowIfNull(overlapped);

            // Note: we explicitly allow FreeNativeOverlapped calls after the ThreadPoolBoundHandle has been Disposed.

            ThreadPoolBoundHandleOverlapped wrapper = GetOverlappedWrapper(overlapped);

            if (wrapper._boundHandle != this)
                throw new ArgumentException(SR.Argument_NativeOverlappedWrongBoundHandle, nameof(overlapped));

            if (wrapper._preAllocated != null)
                wrapper._preAllocated.Release();
            else
                Overlapped.Free(overlapped);
        }

        private static unsafe object? GetNativeOverlappedStateCore(NativeOverlapped* overlapped)
        {
            ArgumentNullException.ThrowIfNull(overlapped);

            ThreadPoolBoundHandleOverlapped wrapper = GetOverlappedWrapper(overlapped);
            Debug.Assert(wrapper._boundHandle != null);
            return wrapper._userState;
        }

        private static unsafe ThreadPoolBoundHandleOverlapped GetOverlappedWrapper(NativeOverlapped* overlapped)
        {
            ThreadPoolBoundHandleOverlapped wrapper;
            try
            {
                wrapper = (ThreadPoolBoundHandleOverlapped)Overlapped.Unpack(overlapped);
            }
            catch (NullReferenceException ex)
            {
                throw new ArgumentException(SR.Argument_NativeOverlappedAlreadyFree, nameof(overlapped), ex);
            }

            return wrapper;
        }
    }
}
