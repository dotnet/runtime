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

namespace Microsoft.Win32.SafeHandles {

    [System.Security.SecurityCritical]  // auto-generated_required
    public sealed class SafeFileHandle: SafeHandleZeroOrMinusOneIsInvalid {

        private SafeFileHandle() : base(true) 
        {
        }

        public SafeFileHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle) {
            SetHandle(preexistingHandle);
        }

        [System.Security.SecurityCritical]
        override protected bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(handle);
        }
    }
}

