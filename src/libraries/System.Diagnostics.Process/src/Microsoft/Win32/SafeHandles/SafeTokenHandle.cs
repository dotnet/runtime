// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Class:  SafeTokenHandle 
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
    internal sealed class SafeTokenHandle : SafeHandle
    {
        internal static SafeTokenHandle InvalidHandle = new SafeTokenHandle(IntPtr.Zero);

        internal SafeTokenHandle() : base(IntPtr.Zero, true) { }

        internal SafeTokenHandle(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        public SafeTokenHandle(IntPtr handle, bool ownsHandle)
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
