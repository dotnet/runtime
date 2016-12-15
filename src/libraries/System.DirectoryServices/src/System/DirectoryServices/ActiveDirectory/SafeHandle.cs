// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Security;

    [SuppressUnmanagedCodeSecurityAttribute()]
    internal sealed class PolicySafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal PolicySafeHandle(IntPtr value) : base(true)
        {
            SetHandle(value);
        }

        override protected bool ReleaseHandle()
        {
            // STATUS_SUCCESS is 0
            return UnsafeNativeMethods.LsaClose(handle) == 0;
        }
    }

    [SuppressUnmanagedCodeSecurityAttribute()]
    internal sealed class LsaLogonProcessSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private LsaLogonProcessSafeHandle() : base(true) { }

        internal LsaLogonProcessSafeHandle(IntPtr value) : base(true)
        {
            SetHandle(value);
        }

        override protected bool ReleaseHandle()
        {
            // STATUS_SUCCESS is 0
            return NativeMethods.LsaDeregisterLogonProcess(handle) == 0;
        }
    }

    [SuppressUnmanagedCodeSecurityAttribute()]
    internal sealed class LoadLibrarySafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private LoadLibrarySafeHandle() : base(true) { }

        internal LoadLibrarySafeHandle(IntPtr value) : base(true)
        {
            SetHandle(value);
        }

        override protected bool ReleaseHandle()
        {
            return UnsafeNativeMethods.FreeLibrary(handle) != 0;
        }
    }
}
