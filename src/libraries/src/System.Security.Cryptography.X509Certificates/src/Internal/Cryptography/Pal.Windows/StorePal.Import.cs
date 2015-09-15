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
        public static IStorePal FromBlob(byte[] rawData, String password, X509KeyStorageFlags keyStorageFlags)
        {
            return FromBlobOrFile(rawData, null, password, keyStorageFlags);
        }

        public static IStorePal FromFile(String fileName, String password, X509KeyStorageFlags keyStorageFlags)
        {
            return FromBlobOrFile(null, fileName, password, keyStorageFlags);
        }

        private static StorePal FromBlobOrFile(byte[] rawData, String fileName, String password, X509KeyStorageFlags keyStorageFlags)
        {
            bool fromFile = fileName != null;

            unsafe
            {
                fixed (byte* pRawData = rawData)
                {
                    fixed (char* pFileName = fileName)
                    {
                        CRYPTOAPI_BLOB blob = new CRYPTOAPI_BLOB(fromFile ? 0 : rawData.Length, pRawData);
                        bool persistKeySet = (0 != (keyStorageFlags & X509KeyStorageFlags.PersistKeySet));
                        PfxCertStoreFlags certStoreFlags = MapKeyStorageFlags(keyStorageFlags);

                        void* pvObject = fromFile ? (void*)pFileName : (void*)&blob;

                        ContentType contentType;
                        SafeCertStoreHandle certStore;
                        if (!Interop.crypt32.CryptQueryObject(
                            fromFile ? CertQueryObjectType.CERT_QUERY_OBJECT_FILE : CertQueryObjectType.CERT_QUERY_OBJECT_BLOB,
                            pvObject,
                            StoreExpectedContentFlags,
                            ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL,
                            0,
                            IntPtr.Zero,
                            out contentType,
                            IntPtr.Zero,
                            out certStore,
                            IntPtr.Zero,
                            IntPtr.Zero
                            ))
                        {
                            throw Marshal.GetLastWin32Error().ToCryptographicException();
                        }

                        if (contentType == ContentType.CERT_QUERY_CONTENT_PFX)
                        {
                            certStore.Dispose();

                            if (fromFile)
                            {
                                rawData = File.ReadAllBytes(fileName);
                            }
                            fixed (byte* pRawData2 = rawData)
                            {
                                CRYPTOAPI_BLOB blob2 = new CRYPTOAPI_BLOB(rawData.Length, pRawData2);
                                certStore = Interop.crypt32.PFXImportCertStore(ref blob2, password, certStoreFlags);
                                if (certStore == null || certStore.IsInvalid)
                                    throw Marshal.GetLastWin32Error().ToCryptographicException();
                            }
                        }


                        if (!persistKeySet)
                        {
                            SafeCertContextHandle pCertContext = null;
                            while (Interop.crypt32.CertEnumCertificatesInStore(certStore, ref pCertContext))
                            {
                                CRYPTOAPI_BLOB nullBlob = new CRYPTOAPI_BLOB(0, null);
                                if (!Interop.crypt32.CertSetCertificateContextProperty(pCertContext, CertContextPropId.CERT_DELETE_KEYSET_PROP_ID, CertSetPropertyFlags.CERT_SET_PROPERTY_INHIBIT_PERSIST_FLAG, &nullBlob))
                                    throw Marshal.GetLastWin32Error().ToCryptographicException();
                            }
                        }

                        return new StorePal(certStore);
                    }
                }
            }
        }

        public static IStorePal FromCertificate(ICertificatePal cert)
        {
            CertificatePal certificatePal = (CertificatePal)cert;

            SafeCertStoreHandle certStore = Interop.crypt32.CertOpenStore(
                CertStoreProvider.CERT_STORE_PROV_MEMORY,
                CertEncodingType.All,
                IntPtr.Zero,
                CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG | CertStoreFlags.CERT_STORE_CREATE_NEW_FLAG | CertStoreFlags.CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG,
                null);
            if (certStore.IsInvalid)
                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();;
            if (!Interop.crypt32.CertAddCertificateContextToStore(certStore, certificatePal.CertContext, CertStoreAddDisposition.CERT_STORE_ADD_ALWAYS, IntPtr.Zero))
                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();;
            return new StorePal(certStore);
        }

        /// <summary>
        /// Note: this factory method creates the store using links to the original certificates rather than copies. This means that any changes to certificate properties
        /// in the store changes the original.
        /// </summary>
        public static IStorePal LinkFromCertificateCollection(X509Certificate2Collection certificates)
        {
            // we always want to use CERT_STORE_ENUM_ARCHIVED_FLAG since we want to preserve the collection in this operation.
            // By default, Archived certificates will not be included.

            SafeCertStoreHandle certStore = Interop.crypt32.CertOpenStore(
                CertStoreProvider.CERT_STORE_PROV_MEMORY,
                CertEncodingType.All,
                IntPtr.Zero,
                CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG | CertStoreFlags.CERT_STORE_CREATE_NEW_FLAG,
                null);
            if (certStore.IsInvalid)
                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();;

            //
            // We use CertAddCertificateLinkToStore to keep a link to the original store, so any property changes get
            // applied to the original store. This has a limit of 99 links per cert context however.
            //

            foreach (X509Certificate2 certificate in certificates)
            {
                SafeCertContextHandle certContext = Interop.crypt32.CertDuplicateCertificateContext(certificate.Handle);
                if (!Interop.crypt32.CertAddCertificateLinkToStore(certStore, certContext, CertStoreAddDisposition.CERT_STORE_ADD_ALWAYS, IntPtr.Zero))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();
            }

            return new StorePal(certStore);
        }

        public static IStorePal FromSystemStore(String storeName, StoreLocation storeLocation, OpenFlags openFlags)
        {
            CertStoreFlags certStoreFlags = MapX509StoreFlags(storeLocation, openFlags);

            SafeCertStoreHandle certStore = Interop.crypt32.CertOpenStore(CertStoreProvider.CERT_STORE_PROV_SYSTEM_W, CertEncodingType.All, IntPtr.Zero, certStoreFlags, storeName);
            if (certStore.IsInvalid)
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            //
            // We want the store to auto-resync when requesting a snapshot so that
            // updates to the store will be taken into account.
            //
            // For compat with desktop, ignoring any failures from this call. (It is pretty unlikely to fail, in any case.)
            //
            bool ignore = Interop.crypt32.CertControlStore(certStore, CertControlStoreFlags.None, CertControlStoreType.CERT_STORE_CTRL_AUTO_RESYNC, IntPtr.Zero);

            return new StorePal(certStore);
        }

        // this method maps a X509KeyStorageFlags enum to a combination of crypto API flags
        private static PfxCertStoreFlags MapKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
        {
            PfxCertStoreFlags dwFlags = 0;
            if ((keyStorageFlags & X509KeyStorageFlags.UserKeySet) == X509KeyStorageFlags.UserKeySet)
                dwFlags |= PfxCertStoreFlags.CRYPT_USER_KEYSET;
            else if ((keyStorageFlags & X509KeyStorageFlags.MachineKeySet) == X509KeyStorageFlags.MachineKeySet)
                dwFlags |= PfxCertStoreFlags.CRYPT_MACHINE_KEYSET;

            if ((keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable)
                dwFlags |= PfxCertStoreFlags.CRYPT_EXPORTABLE;
            if ((keyStorageFlags & X509KeyStorageFlags.UserProtected) == X509KeyStorageFlags.UserProtected)
                dwFlags |= PfxCertStoreFlags.CRYPT_USER_PROTECTED;

            return dwFlags;
        }

        // this method maps X509Store OpenFlags to a combination of crypto API flags
        private static CertStoreFlags MapX509StoreFlags(StoreLocation storeLocation, OpenFlags flags)
        {
            CertStoreFlags dwFlags = 0;
            uint openMode = ((uint)flags) & 0x3;
            switch (openMode)
            {
                case (uint)OpenFlags.ReadOnly:
                    dwFlags |= CertStoreFlags.CERT_STORE_READONLY_FLAG;
                    break;
                case (uint)OpenFlags.MaxAllowed:
                    dwFlags |= CertStoreFlags.CERT_STORE_MAXIMUM_ALLOWED_FLAG;
                    break;
            }

            if ((flags & OpenFlags.OpenExistingOnly) == OpenFlags.OpenExistingOnly)
                dwFlags |= CertStoreFlags.CERT_STORE_OPEN_EXISTING_FLAG;
            if ((flags & OpenFlags.IncludeArchived) == OpenFlags.IncludeArchived)
                dwFlags |= CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG;

            if (storeLocation == StoreLocation.LocalMachine)
                dwFlags |= CertStoreFlags.CERT_SYSTEM_STORE_LOCAL_MACHINE;
            else if (storeLocation == StoreLocation.CurrentUser)
                dwFlags |= CertStoreFlags.CERT_SYSTEM_STORE_CURRENT_USER;

            return dwFlags;
        }

        private const ExpectedContentTypeFlags StoreExpectedContentFlags =
            ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_CERT |
            ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT |
            ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED |
            ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED |
            ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_UNSIGNED |
            ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PFX |
            ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_SERIALIZED_STORE;
    }
}
