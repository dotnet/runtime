// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeBCryptHashHandle : SafeBCryptHandle
    {
        private SafeBCryptHashHandle()
            : base()
        {
        }

        protected sealed override bool ReleaseHandle()
        {
            NTSTATUS ntStatus = Interop.BCrypt.BCryptDestroyHash(handle);
            return ntStatus == NTSTATUS.STATUS_SUCCESS;
        }
    }
}
