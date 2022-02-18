// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class StorePal
    {
        internal static partial IStorePal FromHandle(IntPtr storeHandle)
        {
            throw new PlatformNotSupportedException();
        }

        internal static partial ILoaderPal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            ICertificatePal? singleCert;
            bool ephemeralSpecified = keyStorageFlags.HasFlag(X509KeyStorageFlags.EphemeralKeySet);

            if (OpenSslX509CertificateReader.TryReadX509Der(rawData, out singleCert) ||
                OpenSslX509CertificateReader.TryReadX509Pem(rawData, out singleCert))
            {
                // The single X509 structure methods shouldn't return true and out null, only empty
                // collections have that behavior.
                Debug.Assert(singleCert != null);

                return SingleCertToLoaderPal(singleCert);
            }

            List<ICertificatePal>? certPals;
            Exception? openSslException;

            if (OpenSslPkcsFormatReader.TryReadPkcs7Der(rawData, out certPals) ||
                OpenSslPkcsFormatReader.TryReadPkcs7Pem(rawData, out certPals) ||
                OpenSslPkcsFormatReader.TryReadPkcs12(rawData, password, ephemeralSpecified, out certPals, out openSslException))
            {
                Debug.Assert(certPals != null);

                return ListToLoaderPal(certPals);
            }

            Debug.Assert(openSslException != null);
            throw openSslException;
        }

        internal static partial ILoaderPal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            bool ephemeralSpecified = keyStorageFlags.HasFlag(X509KeyStorageFlags.EphemeralKeySet);

            using (SafeBioHandle bio = Interop.Crypto.BioNewFile(fileName, "rb"))
            {
                Interop.Crypto.CheckValidOpenSslHandle(bio);

                return FromBio(fileName, bio, password, ephemeralSpecified);
            }
        }

        private static ILoaderPal FromBio(
            string fileName,
            SafeBioHandle bio,
            SafePasswordHandle password,
            bool ephemeralSpecified)
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

            // Rewind, try again.
            OpenSslX509CertificateReader.RewindBio(bio, bioPosition);

            // Capture the exception so in case of failure, the call to BioSeek does not override it.
            Exception? openSslException;
            byte[] data = File.ReadAllBytes(fileName);
            if (OpenSslPkcsFormatReader.TryReadPkcs12(data, password, ephemeralSpecified, out certPals, out openSslException))
            {
                return ListToLoaderPal(certPals);
            }

            // Since we aren't going to finish reading, leaving the buffer where it was when we got
            // it seems better than leaving it in some arbitrary other position.
            //
            // Use BioSeek directly for the last seek attempt, because any failure here should instead
            // report the already created (but not yet thrown) exception.
            if (Interop.Crypto.BioSeek(bio, bioPosition) < 0)
            {
                Interop.Crypto.ErrClearError();
            }

            Debug.Assert(openSslException != null);
            throw openSslException;
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

        private static ILoaderPal SingleCertToLoaderPal(ICertificatePal singleCert)
        {
            return new OpenSslSingleCertLoader(singleCert);
        }

        private static ILoaderPal ListToLoaderPal(List<ICertificatePal> certPals)
        {
            return new CertCollectionLoader(certPals);
        }
    }
}
