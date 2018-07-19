// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class SafeCertContextHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // 0 is an Invalid Handle
        internal SafeCertContextHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        internal static SafeCertContextHandle InvalidHandle =>
            SafeHandleCache<SafeCertContextHandle>.GetInvalidHandle(
                () => new SafeCertContextHandle(IntPtr.Zero));

        protected override bool ReleaseHandle() => Interop.Crypt32.CertFreeCertificateContext(handle);
    }

    internal sealed class SafeCertStoreHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // 0 is an Invalid Handle
        internal SafeCertStoreHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        internal static SafeCertStoreHandle InvalidHandle =>
            SafeHandleCache<SafeCertStoreHandle>.GetInvalidHandle(
                () => new SafeCertStoreHandle(IntPtr.Zero));

        protected override bool ReleaseHandle() => Interop.Crypt32.CertCloseStore(handle, 0);
    }
}
