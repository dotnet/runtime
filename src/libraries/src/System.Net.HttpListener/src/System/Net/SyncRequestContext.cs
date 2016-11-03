// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal unsafe class SyncRequestContext : RequestContextBase
    {
        private GCHandle _pinnedHandle;

        internal SyncRequestContext(int size)
        {
            BaseConstruction(Allocate(size));
        }

        private Interop.HttpApi.HTTP_REQUEST* Allocate(int size)
        {
            if (_pinnedHandle.IsAllocated)
            {
                if (RequestBuffer.Length == size)
                {
                    return RequestBlob;
                }
                _pinnedHandle.Free();
            }
            SetBuffer(size);
            if (RequestBuffer == null)
            {
                return null;
            }
            _pinnedHandle = GCHandle.Alloc(RequestBuffer, GCHandleType.Pinned);
            return (Interop.HttpApi.HTTP_REQUEST*)Marshal.UnsafeAddrOfPinnedArrayElement(RequestBuffer, 0);
        }

        internal void Reset(int size)
        {
            SetBlob(Allocate(size));
        }

        protected override void OnReleasePins()
        {
            if (_pinnedHandle.IsAllocated)
            {
                _pinnedHandle.Free();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_pinnedHandle.IsAllocated)
            {
                Debug.Assert(!disposing, "AsyncRequestContext::Dispose()|Must call ReleasePins() before calling Dispose().");
                if (!Environment.HasShutdownStarted || disposing)
                {
                    _pinnedHandle.Free();
                }
            }
            base.Dispose(disposing);
        }
    }
}
