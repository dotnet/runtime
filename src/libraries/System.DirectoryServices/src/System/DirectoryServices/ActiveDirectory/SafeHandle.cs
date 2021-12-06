// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.DirectoryServices.ActiveDirectory
{
    internal sealed class LsaLogonProcessSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public LsaLogonProcessSafeHandle() : base(true) { }

        internal LsaLogonProcessSafeHandle(IntPtr value) : base(true)
        {
            SetHandle(value);
        }

        protected override bool ReleaseHandle() => NativeMethods.LsaDeregisterLogonProcess(handle) == 0;
    }
}
