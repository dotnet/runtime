// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    public sealed class SafeAccessTokenHandle : SafeHandle
    {
        private SafeAccessTokenHandle() : base(IntPtr.Zero, true) { }

        // 0 is an Invalid Handle
        public SafeAccessTokenHandle(IntPtr handle) : base(handle, true) { }

        public static SafeAccessTokenHandle InvalidHandle
        {
            get
            {
                return new SafeAccessTokenHandle(IntPtr.Zero);
            }
        }

        public override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero || handle == new IntPtr(-1);
            }
        }

        protected override bool ReleaseHandle()
        {
            return Interop.Kernel32.CloseHandle(handle);
        }
    }
}
