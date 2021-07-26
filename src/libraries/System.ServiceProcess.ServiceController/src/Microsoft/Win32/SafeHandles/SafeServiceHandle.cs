// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// Used to wrap handles gotten from OpenSCManager or OpenService
    /// </summary>
    internal sealed class SafeServiceHandle : SafeHandle
    {
        public SafeServiceHandle() : base(IntPtr.Zero, true)
        {
        }

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
