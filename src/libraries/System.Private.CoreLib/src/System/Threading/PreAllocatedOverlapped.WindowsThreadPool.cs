// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    public sealed partial class PreAllocatedOverlapped : IDisposable, IDeferredDisposable
    {
        internal readonly unsafe Win32ThreadPoolNativeOverlapped* _overlappedWindowsThreadPool;

        internal unsafe bool IsUserObject(byte[]? buffer) => _overlappedWindowsThreadPool->IsUserObject(buffer);

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
