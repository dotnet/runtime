// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.Apple;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class StorePal
    {
        internal static partial IStorePal FromHandle(IntPtr storeHandle)
        {
            if (storeHandle == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(storeHandle));
            }

            var keychainHandle = new SafeKeychainHandle(storeHandle);
            Interop.CoreFoundation.CFRetain(storeHandle);

            return new AppleKeychainStore(keychainHandle, OpenFlags.MaxAllowed);
        }

        internal static partial ILoaderPal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            X509ContentType contentType = X509Certificate2.GetCertContentType(rawData);

            if (contentType == X509ContentType.Pkcs12)
            {
                if ((keyStorageFlags & X509KeyStorageFlags.EphemeralKeySet) == X509KeyStorageFlags.EphemeralKeySet)
                {
                    throw new PlatformNotSupportedException(SR.Cryptography_X509_NoEphemeralPfx);
                }

                bool exportable = (keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable;

                bool persist =
                    (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == X509KeyStorageFlags.PersistKeySet;

                SafeKeychainHandle keychain = persist
                    ? Interop.AppleCrypto.SecKeychainCopyDefault()
                    : Interop.AppleCrypto.CreateTemporaryKeychain();

                return ImportPkcs12(rawData, password, exportable, ephemeralSpecified: false, keychain);
            }

            SafeCFArrayHandle certs = Interop.AppleCrypto.X509ImportCollection(
                rawData,
                contentType,
                password,
                SafeTemporaryKeychainHandle.InvalidHandle,
                exportable: true);

            return new AppleCertLoader(certs, null);
        }

        private static ILoaderPal ImportPkcs12(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password,
            bool exportable,
            bool ephemeralSpecified,
            SafeKeychainHandle keychain)
        {
            ApplePkcs12Reader reader = new ApplePkcs12Reader(rawData);

            try
            {
                reader.Decrypt(password, ephemeralSpecified);
                return new ApplePkcs12CertLoader(reader, keychain, password, exportable);
            }
            catch
            {
                reader.Dispose();
                keychain.Dispose();
                throw;
            }
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

            switch (storeLocation)
            {
                case StoreLocation.CurrentUser:
                    if (ordinalIgnoreCase.Equals("My", storeName))
                        return AppleKeychainStore.OpenDefaultKeychain(openFlags);
                    if (ordinalIgnoreCase.Equals("Root", storeName))
                        return AppleTrustStore.OpenStore(StoreName.Root, storeLocation, openFlags);
                    if (ordinalIgnoreCase.Equals("Disallowed", storeName))
                        return AppleTrustStore.OpenStore(StoreName.Disallowed, storeLocation, openFlags);
                    return FromCustomKeychainStore(storeName, openFlags);

                case StoreLocation.LocalMachine:
                    if (ordinalIgnoreCase.Equals("My", storeName))
                        return AppleKeychainStore.OpenSystemSharedKeychain(openFlags);
                    if (ordinalIgnoreCase.Equals("Root", storeName))
                        return AppleTrustStore.OpenStore(StoreName.Root, storeLocation, openFlags);
                    if (ordinalIgnoreCase.Equals("Disallowed", storeName))
                        return AppleTrustStore.OpenStore(StoreName.Disallowed, storeLocation, openFlags);
                    break;
            }

            if ((openFlags & OpenFlags.OpenExistingOnly) == OpenFlags.OpenExistingOnly)
                throw new CryptographicException(SR.Cryptography_X509_StoreNotFound);

            string message = SR.Format(
                SR.Cryptography_X509_StoreCannotCreate,
                storeName,
                storeLocation);

            throw new CryptographicException(message, new PlatformNotSupportedException(message));
        }

        private static IStorePal FromCustomKeychainStore(string storeName, OpenFlags openFlags)
        {
            string storePath;

            if (!IsValidStoreName(storeName))
                throw new CryptographicException(SR.Format(SR.Security_InvalidValue, nameof(storeName)));

            storePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Keychains",
                storeName.ToLowerInvariant() + ".keychain");

            return AppleKeychainStore.CreateOrOpenKeychain(storePath, openFlags);
        }

        private static bool IsValidStoreName(string storeName)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(storeName) && Path.GetFileName(storeName) == storeName;
            }
            catch (IOException)
            {
                return false;
            }
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
