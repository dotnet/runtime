// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    public sealed partial class PreAllocatedOverlapped : IDisposable, IDeferredDisposable
    {
        private DeferredDisposableLifetime<PreAllocatedOverlapped> _lifetime;

        [CLSCompliant(false)]
        public PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData) :
            this(callback, state, pinData, flowExecutionContext: true)
        {
        }

        [CLSCompliant(false)]
        public static PreAllocatedOverlapped UnsafeCreate(IOCompletionCallback callback, object? state, object? pinData) =>
            UnsafeCreateCore(callback, state, pinData);

        private unsafe PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData, bool flowExecutionContext)
        {
            ArgumentNullException.ThrowIfNull(callback);

            _overlappedCore = Win32ThreadPoolNativeOverlapped.Allocate(callback, state, pinData, this, flowExecutionContext);
        }

        internal bool AddRef() => AddRefCore();

        internal void Release() => ReleaseCore();

        public void Dispose()
        {
            DisposeCore();
        }

        ~PreAllocatedOverlapped()
        {
            Dispose();
        }

        unsafe void IDeferredDisposable.OnFinalRelease(bool disposed)
        {
            IDeferredDisposableOnFinalReleaseCore(disposed);
        }

    }
}
