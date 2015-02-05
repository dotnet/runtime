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
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeProcessHandle : SafeHandle
    {
        private const int DefaultInvalidHandleValue = -1;

        internal SafeProcessHandle(int processId) : 
            this((IntPtr)processId)
        {
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return ((int)handle) < 0; } // Unix processes have non-negative ID values
        }

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            // Nop.  We don't actually hold handles to a process, as there's no equivalent
            // concept on Unix.  SafeProcessHandle justs wrap a process ID.
            return true;
        }
    }
}
