// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    public sealed partial class PreAllocatedOverlapped : IDisposable, IDeferredDisposable
    {
        internal readonly unsafe Win32ThreadPoolNativeOverlapped* _overlappedCore;

        private static PreAllocatedOverlapped UnsafeCreateCore(IOCompletionCallback callback, object? state, object? pinData) =>
            new PreAllocatedOverlapped(callback, state, pinData, flowExecutionContext: false);

        private bool AddRefCore()
        {
            return _lifetime.AddRef();
        }

        private void ReleaseCore()
        {
            _lifetime.Release(this);
        }

        internal unsafe bool IsUserObject(byte[]? buffer) => _overlappedCore->IsUserObject(buffer);

        private void DisposeCore()
        {
            _lifetime.Dispose(this);
            GC.SuppressFinalize(this);
        }

        private unsafe void IDeferredDisposableOnFinalReleaseCore(bool disposed)
        {
            if (_overlappedCore != null)
            {
                if (disposed)
                    Win32ThreadPoolNativeOverlapped.Free(_overlappedCore);
                else
                    *Win32ThreadPoolNativeOverlapped.ToNativeOverlapped(_overlappedCore) = default(NativeOverlapped);
            }
        }
    }
}
