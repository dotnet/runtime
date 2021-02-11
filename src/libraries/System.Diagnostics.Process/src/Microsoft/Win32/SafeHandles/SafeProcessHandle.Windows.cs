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
    /// <summary>Provides a managed wrapper for a process handle.</summary>
    public sealed partial class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>Closes the handle.</summary>
        /// <returns>On Windows, returns <see langword="true" /> if the handle is closed successfully; <see langword="false" /> otherwise. On Unix, always returns <see langword="true" /></returns>
        /// <remarks>On Windows, to get extended error information, call <see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error()" />.</remarks>
        protected override bool ReleaseHandle()
        {
            return Interop.Kernel32.CloseHandle(handle);
        }
    }
}
