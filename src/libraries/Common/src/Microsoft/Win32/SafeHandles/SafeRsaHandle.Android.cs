// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
    internal sealed class SafeRsaHandle : SafeHandle
    {
        public SafeRsaHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.AndroidCrypto.RsaDestroy(handle);
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

            if (!Interop.AndroidCrypto.RsaUpRef(handle))
            {
                throw new CryptographicException();
            }

            safeHandle.SetHandle(handle);
            return safeHandle;
        }
    }
}
