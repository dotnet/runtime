// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeLsaReturnBufferHandle : SafeBuffer
    {
        public SafeLsaReturnBufferHandle() : base(true) { }

        // 0 is an Invalid Handle
        internal SafeLsaReturnBufferHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            // LsaFreeReturnBuffer returns an NTSTATUS
            return Interop.SspiCli.LsaFreeReturnBuffer(handle) >= 0;
        }
    }
}
