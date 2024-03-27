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
            throw new NotImplementedException($"{nameof(StorePal)}.{nameof(FromHandle)}");
        }

        private static AndroidCertLoader FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, bool readingFromFile, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            X509ContentType contentType = X509Certificate2.GetCertContentType(rawData);
            bool ephemeralSpecified = keyStorageFlags.HasFlag(X509KeyStorageFlags.EphemeralKeySet);

            if (contentType == X509ContentType.Pkcs12)
            {
                X509Certificate.EnforceIterationCountLimit(ref rawData, readingFromFile, password.PasswordProvided);
                ICertificatePal[] certPals = ReadPkcs12Collection(rawData, password, ephemeralSpecified);
                return new AndroidCertLoader(certPals);
            }
            else
            {
                SafeX509Handle[] certs = Interop.AndroidCrypto.X509DecodeCollection(rawData);
                return new AndroidCertLoader(certs);
            }
        }

        internal static partial ILoaderPal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            return FromBlob(rawData, password, readingFromFile: false, keyStorageFlags: keyStorageFlags);
        }

        internal static partial ILoaderPal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            byte[] fileBytes = File.ReadAllBytes(fileName);
            return FromBlob(fileBytes, password, readingFromFile: true, keyStorageFlags: keyStorageFlags);
        }

        internal static partial IExportPal FromCertificate(ICertificatePalCore cert)
        {
            return new AndroidExportProvider(cert);
        }

        internal static partial IExportPal LinkFromCertificateCollection(X509Certificate2Collection certificates)
        {
            return new AndroidExportProvider(certificates);
        }

        internal static partial IStorePal FromSystemStore(string storeName, StoreLocation storeLocation, OpenFlags openFlags)
        {
            bool isReadWrite = (openFlags & OpenFlags.ReadWrite) == OpenFlags.ReadWrite;
            if (isReadWrite && storeLocation == StoreLocation.LocalMachine)
            {
                // All LocalMachine stores are read-only from an Android application's perspective
                throw new CryptographicException(
                    SR.Cryptography_Unix_X509_MachineStoresReadOnly,
                    new PlatformNotSupportedException(SR.Cryptography_Unix_X509_MachineStoresReadOnly));
            }

            StringComparer ordinalIgnoreCase = StringComparer.OrdinalIgnoreCase;
            switch (storeLocation)
            {
                case StoreLocation.CurrentUser:
                {
                    // Matches Unix behaviour of getting a disallowed store that is always empty.
                    if (ordinalIgnoreCase.Equals(X509Store.DisallowedStoreName, storeName))
                    {
                        return new UnsupportedDisallowedStore(openFlags);
                    }

                    if (ordinalIgnoreCase.Equals(X509Store.MyStoreName, storeName))
                    {
                        return AndroidKeyStore.OpenDefault(openFlags);
                    }

                    if (ordinalIgnoreCase.Equals(X509Store.RootStoreName, storeName))
                    {
                        // Android only allows updating the trusted store through the built-in settings application
                        if (isReadWrite)
                        {
                            throw new CryptographicException(SR.Security_AccessDenied);
                        }

                        return new TrustedStore(storeLocation);
                    }
                    break;
                }
                case StoreLocation.LocalMachine:
                {
                    if (ordinalIgnoreCase.Equals(X509Store.RootStoreName, storeName))
                    {
                        return new TrustedStore(storeLocation);
                    }

                    break;
                }
            }

            if ((openFlags & OpenFlags.OpenExistingOnly) == OpenFlags.OpenExistingOnly)
                throw new CryptographicException(SR.Cryptography_X509_StoreNotFound);

            string message = SR.Format(SR.Cryptography_X509_StoreCannotCreate, storeName, storeLocation);
            throw new CryptographicException(message, new PlatformNotSupportedException(message));
        }

        private static ICertificatePal[] ReadPkcs12Collection(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password,
            bool ephemeralSpecified)
        {
            using (var reader = new AndroidPkcs12Reader())
            {
                reader.ParsePkcs12(rawData);
                reader.Decrypt(password, ephemeralSpecified);

                ICertificatePal[] certs = new ICertificatePal[reader.GetCertCount()];
                int idx = 0;
                foreach (UnixPkcs12Reader.CertAndKey certAndKey in reader.EnumerateAll())
                {
                    AndroidCertificatePal pal = (AndroidCertificatePal)certAndKey.Cert!;
                    if (certAndKey.Key != null)
                    {
                        pal.SetPrivateKey(AndroidPkcs12Reader.GetPrivateKey(certAndKey.Key));
                    }

                    certs[idx] = pal;
                    idx++;
                }

                return certs;
            }
        }
    }
}
