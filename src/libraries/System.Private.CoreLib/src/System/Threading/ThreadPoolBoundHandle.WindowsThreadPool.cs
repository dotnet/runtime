// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

namespace System.Threading
{
    //
    // Implementation of ThreadPoolBoundHandle that sits on top of the Win32 ThreadPool
    //
    public partial sealed class ThreadPoolBoundHandle : IDisposable, IDeferredDisposable
    {
        private static unsafe ThreadPoolBoundHandle BindHandleCore(SafeHandle handle)
        {
            ArgumentNullException.ThrowIfNull(handle);

            if (handle.IsClosed || handle.IsInvalid)
                throw new ArgumentException(SR.Argument_InvalidHandle, nameof(handle));

            SafeThreadPoolIOHandle threadPoolHandle = Interop.Kernel32.CreateThreadpoolIo(handle, &OnNativeIOCompleted, IntPtr.Zero, IntPtr.Zero);
            if (threadPoolHandle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.Errors.ERROR_INVALID_HANDLE)         // Bad handle
                    throw new ArgumentException(SR.Argument_InvalidHandle, nameof(handle));

                if (errorCode == Interop.Errors.ERROR_INVALID_PARAMETER)     // Handle already bound or sync handle
                    throw new ArgumentException(SR.Argument_AlreadyBoundOrSyncHandle, nameof(handle));

                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }

            return new ThreadPoolBoundHandle(handle, threadPoolHandle);
        }

        [CLSCompliant(false)]
        private unsafe NativeOverlapped* AllocateNativeOverlappedCore(IOCompletionCallback callback, object? state, object? pinData) =>
            AllocateNativeOverlapped(callback, state, pinData, flowExecutionContext: true);

        [CLSCompliant(false)]
        private unsafe NativeOverlapped* UnsafeAllocateNativeOverlappedCore(IOCompletionCallback callback, object? state, object? pinData) =>
            AllocateNativeOverlapped(callback, state, pinData, flowExecutionContext: false);

        private unsafe NativeOverlapped* AllocateNativeOverlapped(IOCompletionCallback callback, object state, object pinData, bool flowExecutionContext)
        {
            ArgumentNullException.ThrowIfNull(callback);

            AddRef();
            try
            {
                Win32ThreadPoolNativeOverlapped* overlapped = Win32ThreadPoolNativeOverlapped.Allocate(callback, state, pinData, preAllocated: null, flowExecutionContext);
                overlapped->Data._boundHandle = this;

                Interop.Kernel32.StartThreadpoolIo(_threadPoolHandle);

                return Win32ThreadPoolNativeOverlapped.ToNativeOverlapped(overlapped);
            }
            catch
            {
                Release();
                throw;
            }
        }

        [CLSCompliant(false)]
        private unsafe NativeOverlapped* AllocateNativeOverlappedCore(PreAllocatedOverlapped preAllocated)
        {
            ArgumentNullException.ThrowIfNull(preAllocated);

            bool addedRefToThis = false;
            bool addedRefToPreAllocated = false;
            try
            {
                addedRefToThis = AddRef();
                addedRefToPreAllocated = preAllocated.AddRef();

                Win32ThreadPoolNativeOverlapped.OverlappedData data = preAllocated._overlapped->Data;
                if (data._boundHandle != null)
                    throw new ArgumentException(SR.Argument_PreAllocatedAlreadyAllocated, nameof(preAllocated));

                data._boundHandle = this;

                Interop.Kernel32.StartThreadpoolIo(_threadPoolHandle);

                return Win32ThreadPoolNativeOverlapped.ToNativeOverlapped(preAllocated._overlapped);
            }
            catch
            {
                if (addedRefToPreAllocated)
                    preAllocated.Release();
                if (addedRefToThis)
                    Release();
                throw;
            }
        }

        [CLSCompliant(false)]
        private unsafe void FreeNativeOverlappedCore(NativeOverlapped* overlapped)
        {
            ArgumentNullException.ThrowIfNull(overlapped);

            Win32ThreadPoolNativeOverlapped* threadPoolOverlapped = Win32ThreadPoolNativeOverlapped.FromNativeOverlapped(overlapped);
            Win32ThreadPoolNativeOverlapped.OverlappedData data = GetOverlappedData(threadPoolOverlapped, this);

            if (!data._completed)
            {
                Interop.Kernel32.CancelThreadpoolIo(_threadPoolHandle);
                Release();
            }

            data._boundHandle = null;
            data._completed = false;

            if (data._preAllocated != null)
                data._preAllocated.Release();
            else
                Win32ThreadPoolNativeOverlapped.Free(threadPoolOverlapped);
        }

        [CLSCompliant(false)]
        private static unsafe object GetNativeOverlappedStateCore(NativeOverlapped* overlapped)
        {
            ArgumentNullException.ThrowIfNull(overlapped);

            Win32ThreadPoolNativeOverlapped* threadPoolOverlapped = Win32ThreadPoolNativeOverlapped.FromNativeOverlapped(overlapped);
            Win32ThreadPoolNativeOverlapped.OverlappedData data = GetOverlappedData(threadPoolOverlapped, null);

            return data._state;
        }

        private static unsafe Win32ThreadPoolNativeOverlapped.OverlappedData GetOverlappedData(Win32ThreadPoolNativeOverlapped* overlapped, ThreadPoolBoundHandle expectedBoundHandle)
        {
            Win32ThreadPoolNativeOverlapped.OverlappedData data = overlapped->Data;

            if (data._boundHandle == null)
                throw new ArgumentException(SR.Argument_NativeOverlappedAlreadyFree, nameof(overlapped));

            if (expectedBoundHandle != null && data._boundHandle != expectedBoundHandle)
                throw new ArgumentException(SR.Argument_NativeOverlappedWrongBoundHandle, nameof(overlapped));

            return data;
        }

        [UnmanagedCallersOnly]
        private static unsafe void OnNativeIOCompleted(IntPtr instance, IntPtr context, IntPtr overlappedPtr, uint ioResult, nuint numberOfBytesTransferred, IntPtr ioPtr)
        {
            var wrapper = ThreadPoolCallbackWrapper.Enter();
            Win32ThreadPoolNativeOverlapped* overlapped = (Win32ThreadPoolNativeOverlapped*)overlappedPtr;

            ThreadPoolBoundHandle boundHandle = overlapped->Data._boundHandle;
            if (boundHandle == null)
                throw new InvalidOperationException(SR.Argument_NativeOverlappedAlreadyFree);

            boundHandle.Release();

            Win32ThreadPoolNativeOverlapped.CompleteWithCallback(ioResult, (uint)numberOfBytesTransferred, overlapped);
            ThreadPool.IncrementCompletedWorkItemCount();
            wrapper.Exit();
        }

        private bool AddRef()
        {
            return _lifetime.AddRef();
        }

        private void Release()
        {
            _lifetime.Release(this);
        }
    }
}
