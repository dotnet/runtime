// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Class:  SafeProcessHandle 
**
** A wrapper for a process handle
**
** 
===========================================================*/

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    [System.Security.SecurityCriticalAttribute]
    public sealed class SafeProcessHandle : SafeHandle
    {
        internal static SafeProcessHandle InvalidHandle = new SafeProcessHandle(IntPtr.Zero);

        internal SafeProcessHandle() : base(IntPtr.Zero, true) { }

        internal SafeProcessHandle(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        public SafeProcessHandle(IntPtr handle, bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
            SetHandle(handle);
        }

        internal void InitialSetHandle(IntPtr h)
        {
            Debug.Assert(IsInvalid, "Safe handle should only be set once");
            base.handle = h;
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get
            { return handle == new IntPtr(0) || handle == new IntPtr(-1); }
        }

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            return Interop.mincore.CloseHandle(handle);
        }
    }
}
