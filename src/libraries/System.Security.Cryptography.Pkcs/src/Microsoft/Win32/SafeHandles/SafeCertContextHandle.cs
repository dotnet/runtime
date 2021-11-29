// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using static Interop.Crypt32;

#pragma warning disable CA1419 // TODO https://github.com/dotnet/roslyn-analyzers/issues/5232: not intended for use with P/Invoke

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
