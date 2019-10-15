// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using static Interop.Crypt32;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeCertContextHandle : SafeHandle
    {
        internal SafeCertContextHandle(IntPtr handle) :
            base(handle, ownsHandle: true)
        {
        }

        internal unsafe CERT_CONTEXT* DangerousGetCertContext()
        {
            return (CERT_CONTEXT*)DangerousGetHandle();
        }

        public sealed override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected sealed override bool ReleaseHandle()
        {
            Interop.Crypt32.CertFreeCertificateContext(handle); // CertFreeCertificateContext always returns TRUE so no point in checking.
            SetHandle(IntPtr.Zero);
            return true;
        }
    }
}
