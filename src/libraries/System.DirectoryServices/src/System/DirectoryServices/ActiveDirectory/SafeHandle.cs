// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using Microsoft.Win32.SafeHandles;

namespace System.DirectoryServices.ActiveDirectory
{
    internal sealed class PolicySafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal PolicySafeHandle(IntPtr value) : base(true)
        {
            SetHandle(value);
        }

        override protected bool ReleaseHandle() => UnsafeNativeMethods.LsaClose(handle) == 0;
    }

    internal sealed class LsaLogonProcessSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private LsaLogonProcessSafeHandle() : base(true) { }

        internal LsaLogonProcessSafeHandle(IntPtr value) : base(true)
        {
            SetHandle(value);
        }

        override protected bool ReleaseHandle() => NativeMethods.LsaDeregisterLogonProcess(handle) == 0;
    }

    internal sealed class LoadLibrarySafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private LoadLibrarySafeHandle() : base(true) { }

        internal LoadLibrarySafeHandle(IntPtr value) : base(true)
        {
            SetHandle(value);
        }

        override protected bool ReleaseHandle() => UnsafeNativeMethods.FreeLibrary(handle) != 0;
    }
}
