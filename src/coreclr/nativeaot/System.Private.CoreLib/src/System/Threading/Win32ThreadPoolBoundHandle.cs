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
        private readonly SafeHandle _handle;
        private readonly SafeThreadPoolIOHandle _threadPoolHandle;
        private DeferredDisposableLifetime<ThreadPoolBoundHandle> _lifetime;

        private ThreadPoolBoundHandle(SafeHandle handle, SafeThreadPoolIOHandle threadPoolHandle)
        {
            _threadPoolHandle = threadPoolHandle;
            _handle = handle;
        }

        public SafeHandle Handle
        {
            get { return _handle; }
        }

        public static unsafe ThreadPoolBoundHandle BindHandle(SafeHandle handle) => BindHandleCore(handle);

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* AllocateNativeOverlapped(IOCompletionCallback callback, object? state, object? pinData) =>
            AllocateNativeOverlappedCore(callback, state, pinData);

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* UnsafeAllocateNativeOverlapped(IOCompletionCallback callback, object? state, object? pinData) =>
            UnsafeAllocateNativeOverlappedCore(callback, state, pinData, flowExecutionContext: false);

        [CLSCompliant(false)]
        public unsafe NativeOverlapped* AllocateNativeOverlapped(PreAllocatedOverlapped preAllocated) =>
            AllocateNativeOverlappedCore(preAllocated);

        [CLSCompliant(false)]
        public unsafe void FreeNativeOverlapped(NativeOverlapped* overlapped) => FreeNativeOverlappedCore(overlapped);

        [CLSCompliant(false)]
        public static unsafe object GetNativeOverlappedState(NativeOverlapped* overlapped) => GetNativeOverlappedStateCore(overlapped);

        public void Dispose()
        {
            _lifetime.Dispose(this);
            GC.SuppressFinalize(this);
        }

        ~ThreadPoolBoundHandle()
        {
            //
            // During shutdown, don't automatically clean up, because this instance may still be
            // reachable/usable by other code.
            //
            if (!Environment.HasShutdownStarted)
                Dispose();
        }

        void IDeferredDisposable.OnFinalRelease(bool disposed)
        {
            if (disposed)
                _threadPoolHandle.Dispose();
        }
    }
}
