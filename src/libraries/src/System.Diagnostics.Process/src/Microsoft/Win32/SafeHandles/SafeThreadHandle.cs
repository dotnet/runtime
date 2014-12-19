// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Class:  SafeThreadHandle 
**
**
** A wrapper for a thread handle
**
** 
===========================================================*/

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeThreadHandle : SafeHandle
    {
        internal SafeThreadHandle()
            : base(IntPtr.Zero, true)
        {
        }

        internal void InitialSetHandle(IntPtr h)
        {
            Debug.Assert(IsInvalid, "Safe handle should only be set once");
            base.SetHandle(h);
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get
            { return handle == new IntPtr(0) || handle == new IntPtr(-1); }
        }

        override protected bool ReleaseHandle()
        {
            return Interop.mincore.CloseHandle(handle);
        }
    }
}
