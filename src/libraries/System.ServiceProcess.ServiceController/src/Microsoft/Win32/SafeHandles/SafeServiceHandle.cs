// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal class SafeServiceHandle : SafeHandle
    {
        internal SafeServiceHandle(IntPtr handle) : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        public override bool IsInvalid
        {
            get { return DangerousGetHandle() == IntPtr.Zero || DangerousGetHandle() == new IntPtr(-1); }
        }

        protected override bool ReleaseHandle()
        {
            return Interop.Advapi32.CloseServiceHandle(handle);
        }
    }
}
