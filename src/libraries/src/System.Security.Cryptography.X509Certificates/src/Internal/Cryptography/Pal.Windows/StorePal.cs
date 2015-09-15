// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Internal.NativeCrypto;
using Internal.Cryptography;
using Internal.Cryptography.Pal.Native;


using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal : IDisposable, IStorePal
    {
        public void CopyTo(X509Certificate2Collection collection)
        {
            Debug.Assert(collection != null);

            SafeCertContextHandle pCertContext = null;
            while (Interop.crypt32.CertEnumCertificatesInStore(_certStore, ref pCertContext))
            {
                X509Certificate2 cert = new X509Certificate2(pCertContext.DangerousGetHandle());
                collection.Add(cert);
            }
        }

        public void Add(ICertificatePal certificate)
        {
            if (!Interop.crypt32.CertAddCertificateContextToStore(_certStore, ((CertificatePal)certificate).CertContext, CertStoreAddDisposition.CERT_STORE_ADD_REPLACE_EXISTING_INHERIT_PROPERTIES, IntPtr.Zero))
                throw Marshal.GetLastWin32Error().ToCryptographicException();
            return;
        }

        public void Remove(ICertificatePal certificate)
        {
            unsafe
            {
                SafeCertContextHandle existingCertContext = ((CertificatePal)certificate).CertContext;
                SafeCertContextHandle enumCertContext = null;
                CERT_CONTEXT* pCertContext = existingCertContext.CertContext;
                if (!Interop.crypt32.CertFindCertificateInStore(_certStore, CertFindType.CERT_FIND_EXISTING, pCertContext, ref enumCertContext))
                    return; // The certificate is not present in the store, simply return.

                CERT_CONTEXT* pCertContextToDelete = enumCertContext.Disconnect();  // CertDeleteCertificateFromContext always frees the context (even on error)
                if (!Interop.crypt32.CertDeleteCertificateFromStore(pCertContextToDelete))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();

                GC.KeepAlive(existingCertContext);
                return;
            }
        }

        public void Dispose()
        {
            SafeCertStoreHandle certStore = _certStore;
            _certStore = null;
            if (certStore != null)
                certStore.Dispose();
            return;
        }

        internal SafeCertStoreHandle SafeCertStoreHandle
        {
            get { return _certStore; }
        }

        private StorePal(SafeCertStoreHandle certStore)
        {
            _certStore = certStore;
        }

        private SafeCertStoreHandle _certStore;
    }
}
