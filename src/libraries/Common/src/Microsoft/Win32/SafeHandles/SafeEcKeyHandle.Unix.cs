// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeEcKeyHandle : SafeHandle
    {
        private SafeEcKeyHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.EcKeyDestroy(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        internal static SafeEcKeyHandle DuplicateHandle(IntPtr handle)
        {
            Debug.Assert(handle != IntPtr.Zero);

            // Reliability: Allocate the SafeHandle before calling EC_KEY_up_ref so
            // that we don't lose a tracked reference in low-memory situations.
            SafeEcKeyHandle safeHandle = new SafeEcKeyHandle();

            if (!Interop.Crypto.EcKeyUpRef(handle))
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            safeHandle.SetHandle(handle);
            return safeHandle;
        }
    }
}
