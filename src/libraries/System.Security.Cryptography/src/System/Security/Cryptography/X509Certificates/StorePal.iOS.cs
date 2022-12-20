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
            throw new PlatformNotSupportedException($"{nameof(StorePal)}.{nameof(FromHandle)}");
        }

        internal static partial ILoaderPal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            List<ICertificatePal>? certificateList = null;

            AppleCertificatePal.TryDecodePem(
                rawData,
                (derData, contentType) =>
                {
                    certificateList ??= new List<ICertificatePal>();
                    certificateList.Add(AppleCertificatePal.FromDerBlob(derData, contentType, password, keyStorageFlags));
                    return true;
                });

            if (certificateList != null)
            {
                return new CertCollectionLoader(certificateList);
            }

            bool ephemeralSpecified = keyStorageFlags.HasFlag(X509KeyStorageFlags.EphemeralKeySet);
            X509ContentType contentType = AppleCertificatePal.GetDerCertContentType(rawData);

            if (contentType == X509ContentType.Pkcs7)
            {
                throw new CryptographicException(
                    SR.Cryptography_X509_PKCS7_Unsupported,
                    new PlatformNotSupportedException(SR.Cryptography_X509_PKCS7_Unsupported));
            }

            if (contentType == X509ContentType.Pkcs12)
            {
                ApplePkcs12Reader reader = new ApplePkcs12Reader(rawData);

                try
                {
                    reader.Decrypt(password, ephemeralSpecified);
                    return new ApplePkcs12CertLoader(reader, password);
                }
                catch
                {
                    reader.Dispose();
                    throw;
                }
            }

            SafeCFArrayHandle certs = Interop.AppleCrypto.X509ImportCollection(
                rawData,
                contentType,
                password);

            using (certs)
            {
                long longCount = Interop.CoreFoundation.CFArrayGetCount(certs);

                if (longCount > int.MaxValue)
                    throw new CryptographicException();

                int count = (int)longCount;

                // Apple returns things in the opposite order from Windows, so read backwards.
                certificateList = new List<ICertificatePal>(count);
                for (int i = count - 1; i >= 0; i--)
                {
                    IntPtr handle = Interop.CoreFoundation.CFArrayGetValueAtIndex(certs, i);

                    if (handle != IntPtr.Zero)
                    {
                        ICertificatePal? certPal = AppleCertificatePal.FromHandle(handle, throwOnFail: false);

                        if (certPal != null)
                        {
                            certificateList.Add(certPal);
                        }
                    }
                }
            }

            return new CertCollectionLoader(certificateList);
        }

        internal static partial ILoaderPal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            byte[] fileBytes = File.ReadAllBytes(fileName);
            return FromBlob(fileBytes, password, keyStorageFlags);
        }

        internal static partial IExportPal FromCertificate(ICertificatePalCore cert)
        {
            return new AppleCertificateExporter(cert);
        }

        internal static partial IExportPal LinkFromCertificateCollection(X509Certificate2Collection certificates)
        {
            return new AppleCertificateExporter(certificates);
        }

        internal static partial IStorePal FromSystemStore(string storeName, StoreLocation storeLocation, OpenFlags openFlags)
        {
            StringComparer ordinalIgnoreCase = StringComparer.OrdinalIgnoreCase;

            if (ordinalIgnoreCase.Equals("Root", storeName))
            {
                throw new CryptographicException(
                    SR.Cryptography_X509_StoreNotFound,
                    new PlatformNotSupportedException(SR.Cryptography_X509_Store_RootUnsupported));
            }

            if (storeLocation == StoreLocation.CurrentUser)
            {
                if (ordinalIgnoreCase.Equals("My", storeName))
                    return AppleKeychainStore.OpenDefaultKeychain(openFlags);
                if (ordinalIgnoreCase.Equals("Disallowed", storeName))
                    return new UnsupportedDisallowedStore(openFlags);
            }

            if ((openFlags & OpenFlags.OpenExistingOnly) == OpenFlags.OpenExistingOnly)
                throw new CryptographicException(SR.Cryptography_X509_StoreNotFound);

            string message = SR.Format(
                SR.Cryptography_X509_StoreCannotCreate,
                storeName,
                storeLocation);

            throw new CryptographicException(message, new PlatformNotSupportedException(message));
        }

        private static void ReadCollection(SafeCFArrayHandle matches, HashSet<X509Certificate2> collection)
        {
            if (matches.IsInvalid)
            {
                return;
            }

            long count = Interop.CoreFoundation.CFArrayGetCount(matches);

            for (int i = 0; i < count; i++)
            {
                IntPtr handle = Interop.CoreFoundation.CFArrayGetValueAtIndex(matches, i);

                SafeSecCertificateHandle certHandle;
                SafeSecIdentityHandle identityHandle;

                if (Interop.AppleCrypto.X509DemuxAndRetainHandle(handle, out certHandle, out identityHandle))
                {
                    X509Certificate2 cert;

                    if (certHandle.IsInvalid)
                    {
                        certHandle.Dispose();
                        cert = new X509Certificate2(new AppleCertificatePal(identityHandle));
                    }
                    else
                    {
                        identityHandle.Dispose();
                        cert = new X509Certificate2(new AppleCertificatePal(certHandle));
                    }

                    if (!collection.Add(cert))
                    {
                        cert.Dispose();
                    }
                }
            }
        }
    }
}
