// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    [SecurityCritical]
    internal sealed class SafeBignumHandle : SafeHandle
    {
        internal SafeBignumHandle(IntPtr handle, bool ownsHandle)
            :  base(handle, ownsHandle)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.libcrypto.BN_clear_free(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }
}
