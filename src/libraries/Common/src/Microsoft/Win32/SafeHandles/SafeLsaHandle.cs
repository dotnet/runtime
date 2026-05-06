// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeLsaHandle : SafeHandle
    {
        public SafeLsaHandle()
            : base(IntPtr.Zero, true)
        {
        }

        protected sealed override bool ReleaseHandle()
        {
            int ntStatus = Interop.SspiCli.LsaDeregisterLogonProcess(handle);
            return ntStatus == 0;
        }

        public sealed override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }
    }
}
