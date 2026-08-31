// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// SafeHandle for the HCERTSTORE handle defined by crypt32.
    /// </summary>
    internal sealed class SafeCertStoreHandle : SafeCrypt32Handle<SafeCertStoreHandle>
    {
        protected sealed override bool ReleaseHandle()
        {
            bool success = Interop.Crypt32.CertCloseStore(handle, 0);
            return success;
        }
    }
}
