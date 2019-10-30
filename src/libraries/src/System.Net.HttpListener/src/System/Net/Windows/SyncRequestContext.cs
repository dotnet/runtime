// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal unsafe class SyncRequestContext : RequestContextBase
    {
        internal SyncRequestContext(int size)
        {
            BaseConstruction(Allocate(size));
        }

        private Interop.HttpApi.HTTP_REQUEST* Allocate(int newSize)
        {
            if (Size > 0 && Size == newSize)
            {
                return RequestBlob;
            }
            SetBuffer(newSize);

            return RequestBuffer == IntPtr.Zero ? null : (Interop.HttpApi.HTTP_REQUEST*)RequestBuffer.ToPointer();
        }

        internal void Reset(int size)
        {
            SetBlob(Allocate(size));
        }

        protected override void OnReleasePins() { }
    }
}
