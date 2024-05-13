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
        internal static partial ILoaderPal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            return FromBlobOrFile(rawData, null, password, keyStorageFlags);
        }

        internal static partial ILoaderPal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            return FromBlobOrFile(null, fileName, password, keyStorageFlags);
        }

        private static ILoaderPal FromBlobOrFile(ReadOnlySpan<byte> rawData, string? fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            bool fromFile = fileName != null;

            unsafe
            {
                fixed (byte* pRawData = rawData)
                {
                    fixed (char* pFileName = fileName)
                    {
                        Interop.Crypt32.DATA_BLOB blob = new Interop.Crypt32.DATA_BLOB(new IntPtr(pRawData), (uint)(fromFile ? 0 : rawData!.Length));
                        void* pvObject = fromFile ? (void*)pFileName : (void*)&blob;

                        Interop.Crypt32.ContentType contentType;
                        SafeCertStoreHandle certStore;
                        if (!Interop.Crypt32.CryptQueryObject(
                            fromFile ? Interop.Crypt32.CertQueryObjectType.CERT_QUERY_OBJECT_FILE : Interop.Crypt32.CertQueryObjectType.CERT_QUERY_OBJECT_BLOB,
                            pvObject,
                            StoreExpectedContentFlags,
                            Interop.Crypt32.ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL,
                            0,
                            IntPtr.Zero,
                            out contentType,
                            IntPtr.Zero,
                            out certStore,
                            IntPtr.Zero,
                            IntPtr.Zero
                            ))
                        {
                            Exception e = Marshal.GetLastPInvokeError().ToCryptographicException();
                            certStore.Dispose();
                            throw e;
                        }

                        if (contentType == Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PFX)
                        {
                            certStore.Dispose();

                            X509Certificate2Collection coll;

                            try
                            {
                                Pkcs12LoaderLimits limits = X509Certificate.GetPkcs12Limits(fromFile, password);

                                if (fromFile)
                                {
                                    Debug.Assert(fileName is not null);

                                    coll = X509CertificateLoader.LoadPkcs12CollectionFromFile(
                                        fileName,
                                        password.DangerousGetSpan(),
                                        keyStorageFlags,
                                        limits);
                                }
                                else
                                {
                                    coll = X509CertificateLoader.LoadPkcs12Collection(
                                        rawData,
                                        password.DangerousGetSpan(),
                                        keyStorageFlags,
                                        limits);
                                }
                            }
                            catch (Pkcs12LoadLimitExceededException e)
                            {
                                throw new CryptographicException(
                                    SR.Cryptography_X509_PfxWithoutPassword_MaxAllowedIterationsExceeded,
                                    e);
                            }

                            // The PFX-Collection loader for .NET Framework and .NET Core and .NET 5-8 assigned
                            // CERT_CLR_DELETE_KEY_PROP_ID on any certificate loaded when PersistKeySet wasn't asserted,
                            // which was different than the delete-tracking method utilized for single certificate PFX loads.
                            //
                            // The property-based approach meant that `new X509Certificate2(someCert.Handle)` would produce a
                            // second instance that was responsible for deleting the private key, and whenever the first one
                            // was disposed (or finalized) it would delete the key out from under the second.  Since
                            // X509Certificate2Collection.Find produces clones, this made for some "interesting" interactions.
                            //
                            // X509CertificateLoader.LoadPkcs12Collection uses the same .NET/managed-only tracking, without
                            // setting a property on the native representation.
                            //
                            // If, for some reason, we want the old behavior back, we have two choices:
                            // 1) change it in X509CertificateLoader
                            // 2) Transform the returned certificates PALs here.

                            return new CollectionBasedLoader(coll);
                        }

                        return new StorePal(certStore);
                    }
                }
            }
        }

        internal static partial IExportPal FromCertificate(ICertificatePalCore cert)
        {
            CertificatePal certificatePal = (CertificatePal)cert;

            SafeCertStoreHandle certStore = Interop.crypt32.CertOpenStore(
                CertStoreProvider.CERT_STORE_PROV_MEMORY,
                Interop.Crypt32.CertEncodingType.All,
                IntPtr.Zero,
                Interop.Crypt32.CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG | Interop.Crypt32.CertStoreFlags.CERT_STORE_CREATE_NEW_FLAG | Interop.Crypt32.CertStoreFlags.CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG,
                null);

            using (SafeCertContextHandle certContext = certificatePal.GetCertContext())
            {
                if (certStore.IsInvalid ||
                    !Interop.Crypt32.CertAddCertificateLinkToStore(certStore, certContext, Interop.Crypt32.CertStoreAddDisposition.CERT_STORE_ADD_ALWAYS, IntPtr.Zero))
                {
                    Exception e = Marshal.GetHRForLastWin32Error().ToCryptographicException();
                    certStore.Dispose();
                    throw e;
                }
            }

            return new StorePal(certStore);
        }

        /// <summary>
        /// Note: this factory method creates the store using links to the original certificates rather than copies. This means that any changes to certificate properties
        /// in the store changes the original.
        /// </summary>
        internal static partial IExportPal LinkFromCertificateCollection(X509Certificate2Collection certificates)
        {
            // we always want to use CERT_STORE_ENUM_ARCHIVED_FLAG since we want to preserve the collection in this operation.
            // By default, Archived certificates will not be included.

            SafeCertStoreHandle certStore = Interop.crypt32.CertOpenStore(
                CertStoreProvider.CERT_STORE_PROV_MEMORY,
                Interop.Crypt32.CertEncodingType.All,
                IntPtr.Zero,
                Interop.Crypt32.CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG | Interop.Crypt32.CertStoreFlags.CERT_STORE_CREATE_NEW_FLAG,
                null);
            try
            {
                if (certStore.IsInvalid)
                {
                    throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
                }

                //
                // We use CertAddCertificateLinkToStore to keep a link to the original store, so any property changes get
                // applied to the original store. This has a limit of 99 links per cert context however.
                //

                for (int i = 0; i < certificates.Count; i++)
                {
                    using (SafeCertContextHandle certContext = ((CertificatePal)certificates[i].Pal!).GetCertContext())
                    {
                        if (!Interop.Crypt32.CertAddCertificateLinkToStore(certStore, certContext, Interop.Crypt32.CertStoreAddDisposition.CERT_STORE_ADD_ALWAYS, IntPtr.Zero))
                        {
                            throw Marshal.GetLastPInvokeError().ToCryptographicException();
                        }
                    }
                }

                return new StorePal(certStore);
            }
            catch
            {
                certStore.Dispose();
                throw;
            }
        }

        internal static partial IStorePal FromSystemStore(string storeName, StoreLocation storeLocation, OpenFlags openFlags)
        {
            Interop.Crypt32.CertStoreFlags certStoreFlags = MapX509StoreFlags(storeLocation, openFlags);

            SafeCertStoreHandle certStore = Interop.crypt32.CertOpenStore(CertStoreProvider.CERT_STORE_PROV_SYSTEM_W, Interop.Crypt32.CertEncodingType.All, IntPtr.Zero, certStoreFlags, storeName);
            if (certStore.IsInvalid)
            {
                Exception e = Marshal.GetLastPInvokeError().ToCryptographicException();
                certStore.Dispose();
                throw e;
            }

            //
            // We want the store to auto-resync when requesting a snapshot so that
            // updates to the store will be taken into account.
            //
            // For compat with desktop, ignoring any failures from this call. (It is pretty unlikely to fail, in any case.)
            //
            _ = Interop.Crypt32.CertControlStore(certStore, Interop.Crypt32.CertControlStoreFlags.None, Interop.Crypt32.CertControlStoreType.CERT_STORE_CTRL_AUTO_RESYNC, IntPtr.Zero);

            return new StorePal(certStore);
        }

        // this method maps a X509KeyStorageFlags enum to a combination of crypto API flags
        private static Interop.Crypt32.PfxCertStoreFlags MapKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
        {
            Interop.Crypt32.PfxCertStoreFlags dwFlags = 0;
            if ((keyStorageFlags & X509KeyStorageFlags.UserKeySet) == X509KeyStorageFlags.UserKeySet)
                dwFlags |= Interop.Crypt32.PfxCertStoreFlags.CRYPT_USER_KEYSET;
            else if ((keyStorageFlags & X509KeyStorageFlags.MachineKeySet) == X509KeyStorageFlags.MachineKeySet)
                dwFlags |= Interop.Crypt32.PfxCertStoreFlags.CRYPT_MACHINE_KEYSET;

            if ((keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable)
                dwFlags |= Interop.Crypt32.PfxCertStoreFlags.CRYPT_EXPORTABLE;
            if ((keyStorageFlags & X509KeyStorageFlags.UserProtected) == X509KeyStorageFlags.UserProtected)
                dwFlags |= Interop.Crypt32.PfxCertStoreFlags.CRYPT_USER_PROTECTED;

            if ((keyStorageFlags & X509KeyStorageFlags.EphemeralKeySet) == X509KeyStorageFlags.EphemeralKeySet)
                dwFlags |= Interop.Crypt32.PfxCertStoreFlags.PKCS12_NO_PERSIST_KEY | Interop.Crypt32.PfxCertStoreFlags.PKCS12_ALWAYS_CNG_KSP;

            return dwFlags;
        }

        // this method maps X509Store OpenFlags to a combination of crypto API flags
        private static Interop.Crypt32.CertStoreFlags MapX509StoreFlags(StoreLocation storeLocation, OpenFlags flags)
        {
            Interop.Crypt32.CertStoreFlags dwFlags = 0;
            uint openMode = ((uint)flags) & 0x3;
            switch (openMode)
            {
                case (uint)OpenFlags.ReadOnly:
                    dwFlags |= Interop.Crypt32.CertStoreFlags.CERT_STORE_READONLY_FLAG;
                    break;
                case (uint)OpenFlags.MaxAllowed:
                    dwFlags |= Interop.Crypt32.CertStoreFlags.CERT_STORE_MAXIMUM_ALLOWED_FLAG;
                    break;
            }

            if ((flags & OpenFlags.OpenExistingOnly) == OpenFlags.OpenExistingOnly)
                dwFlags |= Interop.Crypt32.CertStoreFlags.CERT_STORE_OPEN_EXISTING_FLAG;
            if ((flags & OpenFlags.IncludeArchived) == OpenFlags.IncludeArchived)
                dwFlags |= Interop.Crypt32.CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG;

            if (storeLocation == StoreLocation.LocalMachine)
                dwFlags |= Interop.Crypt32.CertStoreFlags.CERT_SYSTEM_STORE_LOCAL_MACHINE;
            else if (storeLocation == StoreLocation.CurrentUser)
                dwFlags |= Interop.Crypt32.CertStoreFlags.CERT_SYSTEM_STORE_CURRENT_USER;

            return dwFlags;
        }

        private const Interop.Crypt32.ExpectedContentTypeFlags StoreExpectedContentFlags =
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_CERT |
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT |
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED |
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED |
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_UNSIGNED |
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PFX |
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_SERIALIZED_STORE;
    }
}
