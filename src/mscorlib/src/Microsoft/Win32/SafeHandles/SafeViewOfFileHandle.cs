// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    internal sealed class SafeViewOfFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeViewOfFileHandle() : base(true) {}

        // 0 is an Invalid Handle
        internal SafeViewOfFileHandle(IntPtr handle, bool ownsHandle) : base (ownsHandle) {
            SetHandle(handle);
        }

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

