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
        internal readonly unsafe Win32ThreadPoolNativeOverlapped* _overlapped_core;
        private DeferredDisposableLifetime<PreAllocatedOverlapped> _lifetime;

        [CLSCompliant(false)]
        public PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData) :
            this(callback, state, pinData, flowExecutionContext: true)
        {
        }

        [CLSCompliant(false)]
        public static PreAllocatedOverlapped UnsafeCreate(IOCompletionCallback callback, object? state, object? pinData) =>
            ThreadPool.UseWindowsThreadPool ? UnsafeCreateCore(callback, state, pinData) : UnsafeCreatePortableCore(callback, state, pinData);

        private unsafe PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData, bool flowExecutionContext)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                InitiliazeCore(callback, state, pinData, flowExecutionContext);
            }
            else
            {
                InitializePortableCore(callback, state, pinData, flowExecutionContext);
            }
        }

        internal bool AddRef() => ThreadPool.UseWindowsThreadPool ? AddRefCore() : AddRefPortableCore();

        internal void Release() => ThreadPool.UseWindowsThreadPool ? ReleaseCore() : ReleasePortableCore();

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

        ~PreAllocatedOverlapped()
        {
            Dispose();
        }

        unsafe void IDeferredDisposable.OnFinalRelease(bool disposed)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                IDeferredDisposableOnFinalReleaseCore();
            }
            else
            {
                IDeferredDisposableOnFinalReleasePortableCore(disposed);
            }
        }
    }
}
