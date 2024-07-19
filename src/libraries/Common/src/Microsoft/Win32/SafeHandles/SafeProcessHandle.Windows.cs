// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#if SYSTEM_PRIVATE_CORELIB
    internal
#else
    public
#endif // SYSTEM_PRIVATE_CORELIB
    sealed partial class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        protected override bool ReleaseHandle()
        {
            return Interop.Kernel32.CloseHandle(handle);
        }
    }
}
