// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    [SecurityCritical]
    public sealed partial class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal static readonly SafeProcessHandle InvalidHandle = new SafeProcessHandle();

        internal SafeProcessHandle()
            : this(new IntPtr(DefaultInvalidHandleValue))
        {
        }

        internal SafeProcessHandle(IntPtr handle)
            : this(handle, true)
        {
        }

        public SafeProcessHandle(IntPtr existingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(existingHandle);
        }

        internal void InitialSetHandle(IntPtr h)
        {
            Debug.Assert(IsInvalid, "Safe handle should only be set once");
            base.handle = h;
        }
    }
}
