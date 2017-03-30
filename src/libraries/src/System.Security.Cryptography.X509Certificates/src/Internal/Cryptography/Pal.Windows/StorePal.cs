// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography.Pal.Native;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal : IDisposable, IStorePal, IExportPal, ILoaderPal
    {
        private SafeCertStoreHandle _certStore;

        public static IStorePal FromHandle(IntPtr storeHandle)
        {
            if (storeHandle == IntPtr.Zero)
                throw new ArgumentNullException(nameof(storeHandle));

            SafeCertStoreHandle certStoreHandle = Interop.crypt32.CertDuplicateStore(storeHandle);
            if (certStoreHandle == null || certStoreHandle.IsInvalid)
                throw new CryptographicException(SR.Cryptography_InvalidStoreHandle, nameof(storeHandle));

            var pal = new StorePal(certStoreHandle);
            return pal;
        }

        public void CloneTo(X509Certificate2Collection collection)
        {
            CopyTo(collection);
        }

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
            }
        }

        public void Dispose()
        {
            SafeCertStoreHandle certStore = _certStore;
            _certStore = null;
            if (certStore != null)
                certStore.Dispose();
        }

        internal SafeCertStoreHandle SafeCertStoreHandle
        {
            get {return _certStore; }
        }

        SafeHandle IStorePal.SafeHandle
        {
            get
            {
                if (_certStore == null || _certStore.IsInvalid || _certStore.IsClosed)
                    throw new CryptographicException(SR.Cryptography_X509_StoreNotOpen);
                return _certStore;
            }
        }

        internal StorePal(SafeCertStoreHandle certStore)
        {
            _certStore = certStore;
        }
    }
}
