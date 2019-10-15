// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net
{
    internal sealed unsafe class AsyncRequestContext : RequestContextBase
    {
        private NativeOverlapped* _nativeOverlapped;
        private ThreadPoolBoundHandle _boundHandle;
        private readonly ListenerAsyncResult _result;

#if DEBUG
        private volatile int _nativeOverlappedCounter = 0;
        private volatile int _nativeOverlappedUsed = 0;

        private void DebugRefCountReleaseNativeOverlapped()
        {
            Debug.Assert(Interlocked.Decrement(ref _nativeOverlappedCounter) == 0, "NativeOverlapped released too many times.");
            Interlocked.Decrement(ref _nativeOverlappedUsed);
        }

        private void DebugRefCountAllocNativeOverlapped()
        {
            Debug.Assert(Interlocked.Increment(ref _nativeOverlappedCounter) == 1, "NativeOverlapped allocated without release.");
        }
#endif

        internal AsyncRequestContext(ThreadPoolBoundHandle boundHandle, ListenerAsyncResult result)
        {
            _result = result;
            BaseConstruction(Allocate(boundHandle, 0));
        }

        private Interop.HttpApi.HTTP_REQUEST* Allocate(ThreadPoolBoundHandle boundHandle, uint size)
        {
            uint newSize = size != 0 ? size : RequestBuffer == IntPtr.Zero ? 4096 : Size;
            if (_nativeOverlapped != null)
            {
#if DEBUG
                DebugRefCountReleaseNativeOverlapped();
#endif

                NativeOverlapped* nativeOverlapped = _nativeOverlapped;
                _nativeOverlapped = null;
                _boundHandle.FreeNativeOverlapped(nativeOverlapped);
            }

#if DEBUG
            DebugRefCountAllocNativeOverlapped();
#endif
            SetBuffer(checked((int)newSize));
            _boundHandle = boundHandle;
            _nativeOverlapped = boundHandle.AllocateNativeOverlapped(ListenerAsyncResult.IOCallback, state: _result, pinData: RequestBuffer);

            return (Interop.HttpApi.HTTP_REQUEST*)RequestBuffer.ToPointer();
        }

        internal void Reset(ThreadPoolBoundHandle boundHandle, ulong requestId, uint size)
        {
            SetBlob(Allocate(boundHandle, size));
            RequestBlob->RequestId = requestId;
        }

        protected override void OnReleasePins()
        {
            if (_nativeOverlapped != null)
            {
#if DEBUG
                DebugRefCountReleaseNativeOverlapped();
#endif

                NativeOverlapped* nativeOverlapped = _nativeOverlapped;
                _nativeOverlapped = null;
                _boundHandle.FreeNativeOverlapped(nativeOverlapped);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_nativeOverlapped != null)
            {
                Debug.Assert(!disposing, "AsyncRequestContext::Dispose()|Must call ReleasePins() before calling Dispose().");
                if (!Environment.HasShutdownStarted || disposing)
                {
#if DEBUG
                    DebugRefCountReleaseNativeOverlapped();
#endif
                    _boundHandle.FreeNativeOverlapped(_nativeOverlapped);
                }
            }

            base.Dispose(disposing);
        }

        internal NativeOverlapped* NativeOverlapped
        {
            get
            {
#if DEBUG
                Debug.Assert(Interlocked.Increment(ref _nativeOverlappedUsed) == 1, "NativeOverlapped reused.");
#endif

                return _nativeOverlapped;
            }
        }
    }
}
