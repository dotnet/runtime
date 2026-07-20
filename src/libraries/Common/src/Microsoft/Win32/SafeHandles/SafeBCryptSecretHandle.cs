// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeBCryptSecretHandle : SafeBCryptHandle
    {
        protected sealed override bool ReleaseHandle()
        {
            NTSTATUS ntStatus = Interop.BCrypt.BCryptDestroySecret(handle);
            return ntStatus == NTSTATUS.STATUS_SUCCESS;
        }
    }
}
