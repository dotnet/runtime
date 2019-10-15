// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeBCryptAlgorithmHandle : SafeBCryptHandle
    {
        private SafeBCryptAlgorithmHandle()
            : base()
        {
        }

        protected sealed override bool ReleaseHandle()
        {
            NTSTATUS ntStatus = Interop.BCrypt.BCryptCloseAlgorithmProvider(handle, 0);
            return ntStatus == NTSTATUS.STATUS_SUCCESS;
        }
    }
}
