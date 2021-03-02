// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeEcKeyHandle : SafeKeyHandle
    {
        public SafeEcKeyHandle()
        {
        }

        internal SafeEcKeyHandle(IntPtr ptr)
        {
            SetHandle(ptr);
        }

        protected override bool ReleaseHandle()
        {
            Interop.AndroidCrypto.EcKeyDestroy(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        internal static SafeEcKeyHandle DuplicateHandle(IntPtr handle)
        {
            Debug.Assert(handle != IntPtr.Zero);

            // Reliability: Allocate the SafeHandle before calling EC_KEY_up_ref so
            // that we don't lose a tracked reference in low-memory situations.
            SafeEcKeyHandle safeHandle = new SafeEcKeyHandle();

            if (!Interop.AndroidCrypto.EcKeyUpRef(handle))
            {
                throw new CryptographicException();
            }

            safeHandle.SetHandle(handle);
            return safeHandle;
        }

        internal override SafeEcKeyHandle DuplicateHandle() => DuplicateHandle(DangerousGetHandle());
    }
}
