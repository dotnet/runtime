// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates
{
    internal static class X509Utils
    {
        internal const uint CERT_STORE_ENUM_ARCHIVED_FLAG = 0x00000200;
        internal const uint CERT_STORE_CREATE_NEW_FLAG = 0x00002000;

        internal static SafeCertContextHandle DuplicateCertificateContext(X509Certificate2 certificate)
        {
            SafeCertContextHandle safeCertContext = Interop.Crypt32.CertDuplicateCertificateContext(certificate.Handle);
            GC.KeepAlive(certificate);
            return safeCertContext;
        }

        internal static SafeCertStoreHandle ExportToMemoryStore(X509Certificate2Collection collection)
        {
            SafeCertStoreHandle safeCertStoreHandle = SafeCertStoreHandle.InvalidHandle;

            // we always want to use CERT_STORE_ENUM_ARCHIVED_FLAG since we want to preserve the collection in this operation.
            // By default, Archived certificates will not be included.
            safeCertStoreHandle = Interop.Crypt32.CertOpenStore(
                new IntPtr(Interop.Crypt32.CERT_STORE_PROV_MEMORY),
                Interop.Crypt32.X509_ASN_ENCODING | Interop.Crypt32.PKCS_7_ASN_ENCODING,
                IntPtr.Zero,
                CERT_STORE_ENUM_ARCHIVED_FLAG | CERT_STORE_CREATE_NEW_FLAG,
                null);

            if (safeCertStoreHandle == null || safeCertStoreHandle.IsInvalid)
                throw new CryptographicException(Marshal.GetLastWin32Error());

            // We use CertAddCertificateLinkToStore to keep a link to the original store, so any property changes get
            // applied to the original store. This has a limit of 99 links per cert context however.          
            foreach (X509Certificate2 x509 in collection)
            {
                using (SafeCertContextHandle handle = DuplicateCertificateContext(x509))
                {
                    if (!Interop.Crypt32.CertAddCertificateLinkToStore(
                        safeCertStoreHandle,
                        handle,
                        Interop.Crypt32.CERT_STORE_ADD_ALWAYS,
                        SafeCertContextHandle.InvalidHandle))
                    {
                        throw new CryptographicException(Marshal.GetLastWin32Error());
                    }
                }
            }

            return safeCertStoreHandle;
        }

        internal static X509Certificate2Collection GetCertificates(SafeCertStoreHandle safeCertStoreHandle)
        {
            X509Certificate2Collection collection = new X509Certificate2Collection();
            IntPtr pEnumContext = Interop.Crypt32.CertEnumCertificatesInStore(safeCertStoreHandle, IntPtr.Zero);
            while (pEnumContext != IntPtr.Zero)
            {
                X509Certificate2 certificate = new X509Certificate2(pEnumContext);
                collection.Add(certificate);
                pEnumContext = Interop.Crypt32.CertEnumCertificatesInStore(safeCertStoreHandle, pEnumContext);
            }

            return collection;
        }
    }
}
