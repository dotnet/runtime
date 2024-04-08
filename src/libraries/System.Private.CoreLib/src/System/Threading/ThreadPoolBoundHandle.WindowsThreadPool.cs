// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    //
    // Implementation of ThreadPoolBoundHandle that sits on top of the Win32 ThreadPool
    //
    public sealed partial class ThreadPoolBoundHandle : IDisposable, IDeferredDisposable
    {
        private static unsafe ThreadPoolBoundHandle BindHandleWindowsThreadPool(SafeHandle handle)
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

        private unsafe NativeOverlapped* AllocateNativeOverlappedWindowsThreadPool(IOCompletionCallback callback, object? state, object? pinData) =>
            AllocateNativeOverlappedWindowsThreadPool(callback, state, pinData, flowExecutionContext: true);

        private unsafe NativeOverlapped* UnsafeAllocateNativeOverlappedWindowsThreadPool(IOCompletionCallback callback, object? state, object? pinData) =>
            AllocateNativeOverlappedWindowsThreadPool(callback, state, pinData, flowExecutionContext: false);

        private unsafe NativeOverlapped* AllocateNativeOverlappedWindowsThreadPool(IOCompletionCallback callback, object? state, object? pinData, bool flowExecutionContext)
        {
            ArgumentNullException.ThrowIfNull(callback);

            AddRef();
            try
            {
                Win32ThreadPoolNativeOverlapped* overlapped = Win32ThreadPoolNativeOverlapped.Allocate(callback, state, pinData, preAllocated: null, flowExecutionContext);
                overlapped->Data._boundHandle = this;

                if (NativeRuntimeEventSource.Log.IsEnabled())
                    NativeRuntimeEventSource.Log.ThreadPoolIOEnqueue(Win32ThreadPoolNativeOverlapped.ToNativeOverlapped(overlapped));

                Interop.Kernel32.StartThreadpoolIo(_threadPoolHandle!);

                return Win32ThreadPoolNativeOverlapped.ToNativeOverlapped(overlapped);
            }
            catch
            {
                Release();
                throw;
            }
        }

        private unsafe NativeOverlapped* AllocateNativeOverlappedWindowsThreadPool(PreAllocatedOverlapped preAllocated)
        {
            ArgumentNullException.ThrowIfNull(preAllocated);

            bool addedRefToThis = false;
            bool addedRefToPreAllocated = false;
            try
            {
                addedRefToThis = AddRef();
                addedRefToPreAllocated = preAllocated.AddRef();

                Win32ThreadPoolNativeOverlapped.OverlappedData data = preAllocated._overlappedWindowsThreadPool->Data;
                if (data._boundHandle != null)
                    throw new ArgumentException(SR.Argument_PreAllocatedAlreadyAllocated, nameof(preAllocated));

                data._boundHandle = this;

                if (NativeRuntimeEventSource.Log.IsEnabled())
                    NativeRuntimeEventSource.Log.ThreadPoolIOEnqueue(Win32ThreadPoolNativeOverlapped.ToNativeOverlapped(preAllocated._overlappedWindowsThreadPool));

                Interop.Kernel32.StartThreadpoolIo(_threadPoolHandle!);

                return Win32ThreadPoolNativeOverlapped.ToNativeOverlapped(preAllocated._overlappedWindowsThreadPool);
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

        private unsafe void FreeNativeOverlappedWindowsThreadPool(NativeOverlapped* overlapped)
        {
            ArgumentNullException.ThrowIfNull(overlapped);

            Win32ThreadPoolNativeOverlapped* threadPoolOverlapped = Win32ThreadPoolNativeOverlapped.FromNativeOverlapped(overlapped);
            Win32ThreadPoolNativeOverlapped.OverlappedData data = GetOverlappedData(threadPoolOverlapped, this);

            if (!data._completed)
            {
                Interop.Kernel32.CancelThreadpoolIo(_threadPoolHandle!);
                Release();
            }

            data._boundHandle = null;
            data._completed = false;

            if (data._preAllocated != null)
                data._preAllocated.Release();
            else
                Win32ThreadPoolNativeOverlapped.Free(threadPoolOverlapped);
        }

        private static unsafe object? GetNativeOverlappedStateWindowsThreadPool(NativeOverlapped* overlapped)
        {
            ArgumentNullException.ThrowIfNull(overlapped);

            Win32ThreadPoolNativeOverlapped* threadPoolOverlapped = Win32ThreadPoolNativeOverlapped.FromNativeOverlapped(overlapped);
            Win32ThreadPoolNativeOverlapped.OverlappedData data = GetOverlappedData(threadPoolOverlapped, null);

            return data._state;
        }

        private static unsafe Win32ThreadPoolNativeOverlapped.OverlappedData GetOverlappedData(Win32ThreadPoolNativeOverlapped* overlapped, ThreadPoolBoundHandle? expectedBoundHandle)
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

            ThreadPoolBoundHandle? boundHandle = overlapped->Data._boundHandle;
            if (boundHandle == null)
                throw new InvalidOperationException(SR.Argument_NativeOverlappedAlreadyFree);

            boundHandle.Release();

            if (NativeRuntimeEventSource.Log.IsEnabled())
                NativeRuntimeEventSource.Log.ThreadPoolIODequeue(Win32ThreadPoolNativeOverlapped.ToNativeOverlapped(overlapped));

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

        private void DisposeWindowsThreadPool()
        {
            _lifetime.Dispose(this);
            GC.SuppressFinalize(this);
        }

        private void FinalizeWindowsThreadPool()
        {
            //
            // During shutdown, don't automatically clean up, because this instance may still be
            // reachable/usable by other code.
            //
            if (!Environment.HasShutdownStarted)
                Dispose();
        }

        private void IDeferredDisposableOnFinalReleaseWindowsThreadPool(bool disposed)
        {
            if (disposed)
                _threadPoolHandle!.Dispose();
        }
    }
}
