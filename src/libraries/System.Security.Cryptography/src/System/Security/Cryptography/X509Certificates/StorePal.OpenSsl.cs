// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class StorePal
    {
#pragma warning disable IDE0060
        internal static partial IStorePal FromHandle(IntPtr storeHandle)
        {
            throw new PlatformNotSupportedException();
        }
#pragma warning restore IDE0060

        internal static partial ILoaderPal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            ICertificatePal? singleCert;

            if (OpenSslX509CertificateReader.TryReadX509Der(rawData, out singleCert) ||
                OpenSslX509CertificateReader.TryReadX509Pem(rawData, out singleCert))
            {
                // The single X509 structure methods shouldn't return true and out null, only empty
                // collections have that behavior.
                Debug.Assert(singleCert != null);

                return SingleCertToLoaderPal(singleCert);
            }

            List<ICertificatePal>? certPals;

            if (OpenSslPkcsFormatReader.TryReadPkcs7Der(rawData, out certPals) ||
                OpenSslPkcsFormatReader.TryReadPkcs7Pem(rawData, out certPals))
            {
                Debug.Assert(certPals != null);

                return ListToLoaderPal(certPals);
            }

            try
            {
                return new CollectionBasedLoader(
                    X509CertificateLoader.LoadPkcs12Collection(
                        rawData,
                        password.DangerousGetSpan(),
                        keyStorageFlags,
                        X509Certificate.GetPkcs12Limits(fromFile: false, password)));
            }
            catch (Pkcs12LoadLimitExceededException e)
            {
                throw new CryptographicException(
                    SR.Cryptography_X509_PfxWithoutPassword_MaxAllowedIterationsExceeded,
                    e);
            }
        }

        internal static partial ILoaderPal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            using (SafeBioHandle bio = Interop.Crypto.BioNewFile(fileName, "rb"))
            {
                Interop.Crypto.CheckValidOpenSslHandle(bio);

                return FromBio(fileName, bio, password, keyStorageFlags);
            }
        }

        private static ILoaderPal FromBio(
            string fileName,
            SafeBioHandle bio,
            SafePasswordHandle password,
            X509KeyStorageFlags keyStorageFlags)
        {
            int bioPosition = Interop.Crypto.BioTell(bio);
            Debug.Assert(bioPosition >= 0);

            ICertificatePal? singleCert;

            if (OpenSslX509CertificateReader.TryReadX509Pem(bio, out singleCert))
            {
                return SingleCertToLoaderPal(singleCert);
            }

            // Rewind, try again.
            OpenSslX509CertificateReader.RewindBio(bio, bioPosition);

            if (OpenSslX509CertificateReader.TryReadX509Der(bio, out singleCert))
            {
                return SingleCertToLoaderPal(singleCert);
            }

            // Rewind, try again.
            OpenSslX509CertificateReader.RewindBio(bio, bioPosition);

            List<ICertificatePal>? certPals;

            if (OpenSslPkcsFormatReader.TryReadPkcs7Pem(bio, out certPals))
            {
                return ListToLoaderPal(certPals);
            }

            // Rewind, try again.
            OpenSslX509CertificateReader.RewindBio(bio, bioPosition);

            if (OpenSslPkcsFormatReader.TryReadPkcs7Der(bio, out certPals))
            {
                return ListToLoaderPal(certPals);
            }

            try
            {
                return new CollectionBasedLoader(
                    X509CertificateLoader.LoadPkcs12CollectionFromFile(
                        fileName,
                        password.DangerousGetSpan(),
                        keyStorageFlags,
                        X509Certificate.GetPkcs12Limits(fromFile: true, password)));
            }
            catch (Pkcs12LoadLimitExceededException e)
            {
                throw new CryptographicException(
                    SR.Cryptography_X509_PfxWithoutPassword_MaxAllowedIterationsExceeded,
                    e);
            }
        }

        internal static partial IExportPal FromCertificate(ICertificatePalCore cert)
        {
            return new OpenSslExportProvider(cert);
        }

        internal static partial IExportPal LinkFromCertificateCollection(X509Certificate2Collection certificates)
        {
            return new OpenSslExportProvider(certificates);
        }

        internal static partial IStorePal FromSystemStore(string storeName, StoreLocation storeLocation, OpenFlags openFlags)
        {
            if (storeLocation == StoreLocation.CurrentUser)
            {
                if (X509Store.DisallowedStoreName.Equals(storeName, StringComparison.OrdinalIgnoreCase))
                {
                    return OpenSslDirectoryBasedStoreProvider.OpenDisallowedStore(openFlags);
                }

                return new OpenSslDirectoryBasedStoreProvider(storeName, openFlags);
            }

            Debug.Assert(storeLocation == StoreLocation.LocalMachine);

            if ((openFlags & OpenFlags.ReadWrite) == OpenFlags.ReadWrite)
            {
                throw new CryptographicException(
                    SR.Cryptography_Unix_X509_MachineStoresReadOnly,
                    new PlatformNotSupportedException(SR.Cryptography_Unix_X509_MachineStoresReadOnly));
            }

            // The static store approach here is making an optimization based on not
            // having write support.  Once writing is permitted the stores would need
            // to fresh-read whenever being requested.

            if (X509Store.RootStoreName.Equals(storeName, StringComparison.OrdinalIgnoreCase))
            {
                return OpenSslCachedSystemStoreProvider.MachineRoot;
            }

            if (X509Store.IntermediateCAStoreName.Equals(storeName, StringComparison.OrdinalIgnoreCase))
            {
                return OpenSslCachedSystemStoreProvider.MachineIntermediate;
            }

            throw new CryptographicException(
                SR.Cryptography_Unix_X509_MachineStoresRootOnly,
                new PlatformNotSupportedException(SR.Cryptography_Unix_X509_MachineStoresRootOnly));
        }

        private static OpenSslSingleCertLoader SingleCertToLoaderPal(ICertificatePal singleCert) => new OpenSslSingleCertLoader(singleCert);

        private static CertCollectionLoader ListToLoaderPal(List<ICertificatePal> certPals) => new CertCollectionLoader(certPals);
    }
}
