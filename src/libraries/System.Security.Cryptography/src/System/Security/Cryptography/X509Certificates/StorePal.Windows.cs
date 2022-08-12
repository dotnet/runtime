// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class StorePal : IDisposable, IStorePal, IExportPal, ILoaderPal
    {
        private SafeCertStoreHandle _certStore;

        internal static partial IStorePal FromHandle(IntPtr storeHandle)
        {
            if (storeHandle == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(storeHandle));
            }

            SafeCertStoreHandle certStoreHandle = Interop.Crypt32.CertDuplicateStore(storeHandle);
            if (certStoreHandle == null || certStoreHandle.IsInvalid)
            {
                certStoreHandle?.Dispose();
                throw new CryptographicException(SR.Cryptography_InvalidStoreHandle, nameof(storeHandle));
            }

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

            SafeCertContextHandle? pCertContext = null;
            while (Interop.crypt32.CertEnumCertificatesInStore(_certStore, ref pCertContext))
            {
                X509Certificate2 cert = new X509Certificate2(pCertContext.DangerousGetHandle());
                collection.Add(cert);
            }
        }

        public void Add(ICertificatePal certificate)
        {
            using (SafeCertContextHandle certContext = ((CertificatePal)certificate).GetCertContext())
            {
                if (!Interop.Crypt32.CertAddCertificateContextToStore(_certStore, certContext, Interop.Crypt32.CertStoreAddDisposition.CERT_STORE_ADD_REPLACE_EXISTING_INHERIT_PROPERTIES, IntPtr.Zero))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();
            }
        }

        public unsafe void Remove(ICertificatePal certificate)
        {
            using (SafeCertContextHandle existingCertContext = ((CertificatePal)certificate).GetCertContext())
            {
                SafeCertContextHandle? enumCertContext = null;
                Interop.Crypt32.CERT_CONTEXT* pCertContext = existingCertContext.CertContext;
                if (!Interop.crypt32.CertFindCertificateInStore(_certStore, Interop.Crypt32.CertFindType.CERT_FIND_EXISTING, pCertContext, ref enumCertContext))
                    return; // The certificate is not present in the store, simply return.

                Interop.Crypt32.CERT_CONTEXT* pCertContextToDelete = enumCertContext.Disconnect();  // CertDeleteCertificateFromContext always frees the context (even on error)
                enumCertContext.Dispose();

                if (!Interop.Crypt32.CertDeleteCertificateFromStore(pCertContextToDelete))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();
            }
        }

        public void Dispose()
        {
            SafeCertStoreHandle? certStore = _certStore;
            if (certStore != null)
            {
                _certStore = null!;
                certStore.Dispose();
            }
        }

        internal SafeCertStoreHandle SafeCertStoreHandle
        {
            get { return _certStore; }
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
