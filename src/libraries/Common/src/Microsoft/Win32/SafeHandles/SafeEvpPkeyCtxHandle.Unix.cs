// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeEvpPKeyCtxHandle : SafeHandle
    {
        public SafeEvpPKeyCtxHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        public SafeEvpPKeyCtxHandle(IntPtr handle, bool ownsHandle)
            : base(handle, ownsHandle)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.EvpPKeyCtxDestroy(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
