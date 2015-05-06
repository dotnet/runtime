// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            return Interop.mincore.CloseHandle(handle);
        }
    }
}
