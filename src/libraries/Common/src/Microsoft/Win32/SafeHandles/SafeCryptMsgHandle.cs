// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// SafeHandle for the HCRYPTMSG handle defined by crypt32.
    /// </summary>
    internal sealed class SafeCryptMsgHandle : SafeCrypt32Handle<SafeCryptMsgHandle>
    {
        protected sealed override bool ReleaseHandle()
        {
            bool success = Interop.Crypt32.CryptMsgClose(handle);
            return success;
        }
    }
}
