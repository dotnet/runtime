// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    public sealed class PreAllocatedOverlapped : IDisposable, IDeferredDisposable
    {
        internal readonly unsafe Win32ThreadPoolNativeOverlapped* _overlapped;
        private DeferredDisposableLifetime<PreAllocatedOverlapped> _lifetime;

        [CLSCompliant(false)]
        public PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData) :
            this(callback, state, pinData, flowExecutionContext: true)
        {
        }

        [CLSCompliant(false)]
        public static PreAllocatedOverlapped UnsafeCreate(IOCompletionCallback callback, object? state, object? pinData) => UnsafeCreateCore(callback, state, pinData);

        internal bool AddRef() => AddRefCore();

        internal void Release() => ReleaseCore();

        public void Dispose()
        {
            _lifetime.Dispose(this);
            GC.SuppressFinalize(this);
        }

        ~PreAllocatedOverlapped()
        {
            Dispose();
        }

        unsafe void IDeferredDisposable.OnFinalRelease(bool disposed)
        {
            if (_overlapped != null)
            {
                if (disposed)
                    Win32ThreadPoolNativeOverlapped.Free(_overlapped);
                else
                    *Win32ThreadPoolNativeOverlapped.ToNativeOverlapped(_overlapped) = default(NativeOverlapped);
            }
        }

        internal unsafe bool IsUserObject(byte[]? buffer) => IsUserObjectCore(buffer);
    }
}
