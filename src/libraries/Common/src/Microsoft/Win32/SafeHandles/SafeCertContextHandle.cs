// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1852 // some projects have types deriving from this

using System;
using System.Diagnostics;
using static Interop.Crypt32;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// SafeHandle for the CERT_CONTEXT structure defined by crypt32.
    /// </summary>
    internal class SafeCertContextHandle : SafeCrypt32Handle<SafeCertContextHandle>
    {
        private SafeCertContextHandle? _parent;

        public SafeCertContextHandle() { }

        public SafeCertContextHandle(SafeCertContextHandle parent)
        {
            ArgumentNullException.ThrowIfNull(parent);

            Debug.Assert(!parent.IsInvalid);
            Debug.Assert(!parent.IsClosed);

            bool ignored = false;
            parent.DangerousAddRef(ref ignored);
            _parent = parent;

            SetHandle(_parent.handle);
        }

        protected override bool ReleaseHandle()
        {
            if (_parent != null)
            {
                _parent.DangerousRelease();
                _parent = null;
            }
            else
            {
                CertFreeCertificateContext(handle);
            }

            SetHandle(IntPtr.Zero);
            return true;
        }

        public unsafe CERT_CONTEXT* CertContext
        {
            get { return (CERT_CONTEXT*)handle; }
        }

        // Extract the raw CERT_CONTEXT* pointer and reset the SafeHandle to the invalid state so it no longer auto-destroys the CERT_CONTEXT.
        public unsafe CERT_CONTEXT* Disconnect()
        {
            CERT_CONTEXT* pCertContext = (CERT_CONTEXT*)handle;
            SetHandle(IntPtr.Zero);
            return pCertContext;
        }

        public bool HasPersistedPrivateKey
        {
            get { return CertHasProperty(CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID); }
        }

        public bool HasEphemeralPrivateKey
        {
            get { return CertHasProperty(CertContextPropId.CERT_KEY_CONTEXT_PROP_ID); }
        }

        public bool ContainsPrivateKey
        {
            get { return HasPersistedPrivateKey || HasEphemeralPrivateKey; }
        }

        public SafeCertContextHandle Duplicate()
        {
            return CertDuplicateCertificateContext(handle);
        }

        private bool CertHasProperty(CertContextPropId propertyId)
        {
            int cb = 0;
            bool hasProperty = CertGetCertificateContextProperty(
                this,
                propertyId,
                null,
                ref cb);

            return hasProperty;
        }
    }
}
