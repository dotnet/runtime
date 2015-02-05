// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            return Interop.mincore.CloseServiceHandle(handle);
        }
    }
}