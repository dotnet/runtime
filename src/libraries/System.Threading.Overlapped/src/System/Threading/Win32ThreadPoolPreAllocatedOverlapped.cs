// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Threading
{
    public sealed class PreAllocatedOverlapped : IDisposable, IDeferredDisposable
    {
        internal unsafe readonly Win32ThreadPoolNativeOverlapped* _overlapped;
        private DeferredDisposableLifetime<PreAllocatedOverlapped> _lifetime;

        [CLSCompliant(false)]
        public unsafe PreAllocatedOverlapped(IOCompletionCallback callback, object state, object pinData)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            _overlapped = Win32ThreadPoolNativeOverlapped.Allocate(callback, state, pinData, this);
        }

        internal bool AddRef()
        {
            return _lifetime.AddRef(this);
        }

        internal void Release()
        {
            _lifetime.Release(this);
        }

        public void Dispose()
        {
            _lifetime.Dispose(this);
            GC.SuppressFinalize(this);
        }

        ~PreAllocatedOverlapped()
        {
            //
            // During shutdown, don't automatically clean up, because this instance may still be
            // reachable/usable by other code.
            //
            if (!Environment.HasShutdownStarted)
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
    }
}
