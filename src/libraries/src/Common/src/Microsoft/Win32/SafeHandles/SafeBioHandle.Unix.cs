// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    [SecurityCritical]
    internal sealed class SafeBioHandle : SafeHandle
    {
        private SafeBioHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.libcrypto.BIO_free(handle);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }
}
