// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal unsafe class SyncRequestContext : RequestContextBase
    {
        private GCHandle m_PinnedHandle;

        internal SyncRequestContext(int size)
        {
            BaseConstruction(Allocate(size));
        }

        private Interop.HttpApi.HTTP_REQUEST* Allocate(int size)
        {
            if (m_PinnedHandle.IsAllocated)
            {
                if (RequestBuffer.Length == size)
                {
                    return RequestBlob;
                }
                m_PinnedHandle.Free();
            }
            SetBuffer(size);
            if (RequestBuffer == null)
            {
                return null;
            }
            m_PinnedHandle = GCHandle.Alloc(RequestBuffer, GCHandleType.Pinned);
            return (Interop.HttpApi.HTTP_REQUEST*)Marshal.UnsafeAddrOfPinnedArrayElement(RequestBuffer, 0);
        }

        internal void Reset(int size)
        {
            SetBlob(Allocate(size));
        }

        protected override void OnReleasePins()
        {
            if (m_PinnedHandle.IsAllocated)
            {
                m_PinnedHandle.Free();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (m_PinnedHandle.IsAllocated)
            {
                Debug.Assert(!disposing, "AsyncRequestContext::Dispose()|Must call ReleasePins() before calling Dispose().");
                if (!Environment.HasShutdownStarted || disposing)
                {
                    m_PinnedHandle.Free();
                }
            }
            base.Dispose(disposing);
        }
    }
}
