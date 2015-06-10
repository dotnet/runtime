// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    [SecurityCritical]
    internal sealed class SafeX509Handle : SafeHandle
    {
        private SafeX509Handle() : 
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            Interop.libcrypto.X509_free(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return handle == IntPtr.Zero; }
        }
    }
}
