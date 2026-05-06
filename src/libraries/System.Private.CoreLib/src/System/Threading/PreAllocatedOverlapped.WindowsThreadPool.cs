// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    public sealed partial class PreAllocatedOverlapped : IDisposable, IDeferredDisposable
    {
        internal readonly unsafe Win32ThreadPoolNativeOverlapped* _overlappedWindowsThreadPool;

        private static PreAllocatedOverlapped UnsafeCreateWindowsThreadPool(IOCompletionCallback callback, object? state, object? pinData) =>
            new PreAllocatedOverlapped(callback, state, pinData, flowExecutionContext: false);

        private bool AddRefWindowsThreadPool()
        {
            return _lifetime.AddRef();
        }

        private void ReleaseWindowsThreadPool()
        {
            _lifetime.Release(this);
        }

        internal unsafe bool IsUserObject(byte[]? buffer) => _overlappedWindowsThreadPool->IsUserObject(buffer);

        private void DisposeWindowsThreadPool()
        {
            _lifetime.Dispose(this);
            GC.SuppressFinalize(this);
        }

        private unsafe void IDeferredDisposableOnFinalReleaseWindowsThreadPool(bool disposed)
        {
            if (_overlappedWindowsThreadPool != null)
            {
                if (disposed)
                    Win32ThreadPoolNativeOverlapped.Free(_overlappedWindowsThreadPool);
                else
                    *Win32ThreadPoolNativeOverlapped.ToNativeOverlapped(_overlappedWindowsThreadPool) = default(NativeOverlapped);
            }
        }
    }
}
