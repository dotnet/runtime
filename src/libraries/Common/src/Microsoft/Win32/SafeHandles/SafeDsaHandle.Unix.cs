// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeDsaHandle : SafeHandle
    {
        public SafeDsaHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.DsaDestroy(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        internal static SafeDsaHandle DuplicateHandle(IntPtr handle)
        {
            Debug.Assert(handle != IntPtr.Zero);

            // Reliability: Allocate the SafeHandle before calling Dsa_up_ref so
            // that we don't lose a tracked reference in low-memory situations.
            SafeDsaHandle safeHandle = new SafeDsaHandle();

            if (!Interop.Crypto.DsaUpRef(handle))
            {
                Exception e = Interop.Crypto.CreateOpenSslCryptographicException();
                safeHandle.Dispose();
                throw e;
            }

            safeHandle.SetHandle(handle);
            return safeHandle;
        }
    }
}
