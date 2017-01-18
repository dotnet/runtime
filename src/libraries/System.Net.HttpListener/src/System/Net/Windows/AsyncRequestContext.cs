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

        internal AsyncRequestContext(ThreadPoolBoundHandle boundHandle, ListenerAsyncResult result)
        {
            _result = result;
            BaseConstruction(Allocate(boundHandle, 0));
        }

        private Interop.HttpApi.HTTP_REQUEST* Allocate(ThreadPoolBoundHandle boundHandle, uint size)
        {
            uint newSize = size != 0 ? size : RequestBuffer == null ? 4096 : Size;
            if (_nativeOverlapped != null && newSize != RequestBuffer.Length)
            {
                NativeOverlapped* nativeOverlapped = _nativeOverlapped;
                _nativeOverlapped = null;
                _boundHandle.FreeNativeOverlapped(nativeOverlapped);
            }
            if (_nativeOverlapped == null)
            {
                SetBuffer(checked((int)newSize));
                _boundHandle = boundHandle;
                _nativeOverlapped = boundHandle.AllocateNativeOverlapped(ListenerAsyncResult.IOCallback, state: _result, pinData: RequestBuffer);
                return (Interop.HttpApi.HTTP_REQUEST*)Marshal.UnsafeAddrOfPinnedArrayElement(RequestBuffer, 0);
            }
            return RequestBlob;
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
                    _boundHandle.FreeNativeOverlapped(_nativeOverlapped);
                }
            }
            base.Dispose(disposing);
        }

        internal NativeOverlapped* NativeOverlapped
        {
            get
            {
                return _nativeOverlapped;
            }
        }
    }
}

