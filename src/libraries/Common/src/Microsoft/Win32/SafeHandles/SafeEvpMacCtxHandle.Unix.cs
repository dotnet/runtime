// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeEvpMacCtxHandle : SafeHandle
    {
        public SafeEvpMacCtxHandle() : base(0, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.EvpMacCtxFree(handle);
            return true;
        }

        public override bool IsInvalid => handle == 0;
    }
}
