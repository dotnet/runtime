// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
** A wrapper for file handles
**
** 
===========================================================*/
using System;
using System.Security;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Win32.SafeHandles
{
    [System.Security.SecurityCritical]  // auto-generated
    internal sealed class SafeViewOfFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [System.Security.SecurityCritical]  // auto-generated_required
        internal SafeViewOfFileHandle() : base(true) {}

        // 0 is an Invalid Handle
        [System.Security.SecurityCritical]  // auto-generated_required
        internal SafeViewOfFileHandle(IntPtr handle, bool ownsHandle) : base (ownsHandle) {
            SetHandle(handle);
        }

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            if (Win32Native.UnmapViewOfFile(handle))
            {
                handle = IntPtr.Zero;
                return true;
            }
            
            return false;
        }
    }
}

