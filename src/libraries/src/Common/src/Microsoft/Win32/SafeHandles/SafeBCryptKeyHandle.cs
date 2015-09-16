// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
