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
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed partial class SafeTokenHandle : SafeHandle
    {
        private const int DefaultInvalidHandleValue = 0;

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return handle == IntPtr.Zero || handle == new IntPtr(-1); }
        }

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            return Interop.mincore.CloseHandle(handle);
        }
    }
}
