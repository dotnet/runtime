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
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private const int DefaultInvalidHandleValue = -1;

        internal SafeProcessHandle(int processId) : 
            this((IntPtr)processId)
        {
        }

        public override bool IsInvalid
        {
            get { return ((int)handle) < 0; } // Unix processes have non-negative ID values
        }

        protected override bool ReleaseHandle()
        {
            // Nop.  We don't actually hold handles to a process, as there's no equivalent
            // concept on Unix.  SafeProcessHandle justs wrap a process ID.
            return true;
        }
    }
}
