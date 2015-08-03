// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    [SecurityCritical]
    internal sealed class SafeRsaHandle : SafeHandle
    {
        private SafeRsaHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.libcrypto.RSA_free(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        internal static SafeRsaHandle DuplicateHandle(IntPtr handle)
        {
            Debug.Assert(handle != IntPtr.Zero);

            // Reliability: Allocate the SafeHandle before calling RSA_up_ref so
            // that we don't lose a tracked reference in low-memory situations.
            SafeRsaHandle safeHandle = new SafeRsaHandle();

            if (!Interop.libcrypto.RSA_up_ref(handle))
            {
                throw Interop.libcrypto.CreateOpenSslCryptographicException();
            }

            safeHandle.SetHandle(handle);
            return safeHandle;
        }
    }
}
