// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// SafeHandle for the CERT_CONTEXT structure defined by crypt32.
    /// </summary>
    internal sealed class SafeCertContextHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeCertContextHandle()
            : base(ownsHandle: true)
        {
        }

        public SafeCertContextHandle(IntPtr handle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypt32.CertFreeCertificateContext(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        internal bool CertHasProperty(Interop.Crypt32.CertContextPropId propertyId)
        {
            int cb = 0;
            bool hasProperty = Interop.Crypt32.CertGetCertificateContextProperty(
                this,
                propertyId,
                null,
                ref cb);

            return hasProperty;
        }
    }
}
