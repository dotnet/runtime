// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    public sealed class PreAllocatedOverlapped : IDisposable, IDeferredDisposable
    {
        [CLSCompliant(false)]
        private static PreAllocatedOverlapped UnsafeCreateCore(IOCompletionCallback callback, object? state, object? pinData) =>
            new PreAllocatedOverlapped(callback, state, pinData, flowExecutionContext: false);

        private unsafe PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData, bool flowExecutionContext)
        {
            ArgumentNullException.ThrowIfNull(callback);

            _overlapped = Win32ThreadPoolNativeOverlapped.Allocate(callback, state, pinData, this, flowExecutionContext);
        }

        private bool AddRefCore()
        {
            return _lifetime.AddRef();
        }

        private void ReleaseCore()
        {
            _lifetime.Release(this);
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

        private unsafe bool IsUserObjectCore(byte[]? buffer) => _overlapped->IsUserObject(buffer);
    }
}
