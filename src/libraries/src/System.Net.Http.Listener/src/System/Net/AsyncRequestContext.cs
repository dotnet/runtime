// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net
{
    internal unsafe class AsyncRequestContext : RequestContextBase
    {
        internal readonly ThreadPoolBoundHandle m_boundHandle;
        private NativeOverlapped* m_NativeOverlapped;
        private ListenerAsyncResult m_Result;

        internal AsyncRequestContext(ListenerAsyncResult result, ThreadPoolBoundHandle boundHandle)
        {
            m_boundHandle = boundHandle;
            m_Result = result;
            BaseConstruction(Allocate(0));
        }

        private Interop.HttpApi.HTTP_REQUEST* Allocate(uint size)
        {
            uint newSize = size != 0 ? size : RequestBuffer == null ? 4096 : Size;
            if (m_NativeOverlapped != null && newSize != RequestBuffer.Length)
            {
                NativeOverlapped* nativeOverlapped = m_NativeOverlapped;
                m_NativeOverlapped = null;
                m_boundHandle.FreeNativeOverlapped(nativeOverlapped);
            }
            if (m_NativeOverlapped == null)
            {
                SetBuffer(checked((int)newSize));
                m_NativeOverlapped = m_boundHandle.AllocateNativeOverlapped(ListenerAsyncResult.IOCallback, state: m_Result, pinData: RequestBuffer);
                return (Interop.HttpApi.HTTP_REQUEST*)Marshal.UnsafeAddrOfPinnedArrayElement(RequestBuffer, 0);
            }
            return RequestBlob;
        }

        internal void Reset(ulong requestId, uint size)
        {
            SetBlob(Allocate(size));
            RequestBlob->RequestId = requestId;
        }

        protected override void OnReleasePins()
        {
            if (m_NativeOverlapped != null)
            {
                NativeOverlapped* nativeOverlapped = m_NativeOverlapped;
                m_NativeOverlapped = null;
                m_boundHandle.FreeNativeOverlapped(nativeOverlapped);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (m_NativeOverlapped != null)
            {
                Debug.Assert(!disposing, "AsyncRequestContext::Dispose()|Must call ReleasePins() before calling Dispose().");
                if (!Environment.HasShutdownStarted || disposing)
                {
                    m_boundHandle.FreeNativeOverlapped(m_NativeOverlapped);
                }
            }
            base.Dispose(disposing);
        }

        internal NativeOverlapped* NativeOverlapped
        {
            get
            {
                return m_NativeOverlapped;
            }
        }
    }
}

