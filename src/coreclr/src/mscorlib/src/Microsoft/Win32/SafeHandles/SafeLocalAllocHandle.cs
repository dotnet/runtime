// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
namespace Microsoft.Win32.SafeHandles {
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.ConstrainedExecution;

    [System.Security.SecurityCritical]  // auto-generated
    internal sealed class SafeLocalAllocHandle : SafeBuffer {
        private SafeLocalAllocHandle () : base(true) {}

        // 0 is an Invalid Handle
        internal SafeLocalAllocHandle (IntPtr handle) : base (true) {
            SetHandle(handle);
        }

        internal static SafeLocalAllocHandle InvalidHandle {
            get { return new SafeLocalAllocHandle(IntPtr.Zero); }
        }

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            return Win32Native.LocalFree(handle) == IntPtr.Zero;
        }
    }
}
