// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeBCryptKeyHandle : SafeBCryptHandle
    {
        private SafeBCryptKeyHandle()
            : base()
        {
        }

        protected sealed override bool ReleaseHandle()
        {
            NTSTATUS ntStatus = Interop.BCrypt.BCryptDestroyKey(handle);
            return ntStatus == NTSTATUS.STATUS_SUCCESS;
        }
    }
}
