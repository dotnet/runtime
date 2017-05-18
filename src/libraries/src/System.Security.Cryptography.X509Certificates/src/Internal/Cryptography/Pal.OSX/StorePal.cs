// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal
    {
        public static IStorePal FromHandle(IntPtr storeHandle)
        {
            if (storeHandle == IntPtr.Zero)
                throw new ArgumentNullException(nameof(storeHandle));

            var keychainHandle = new SafeKeychainHandle(storeHandle);
            Interop.CoreFoundation.CFRetain(storeHandle);

            return new AppleKeychainStore(keychainHandle, OpenFlags.MaxAllowed);
        }

        public static ILoaderPal FromBlob(byte[] rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            X509ContentType contentType = X509Certificate2.GetCertContentType(rawData);

            SafeKeychainHandle keychain;
            bool exportable = true;

            if (contentType == X509ContentType.Pkcs12)
            {
                if ((keyStorageFlags & X509KeyStorageFlags.EphemeralKeySet) == X509KeyStorageFlags.EphemeralKeySet)
                {
                    throw new PlatformNotSupportedException(SR.Cryptography_X509_NoEphemeralPfx);
                }

                exportable = (keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable;

                bool persist =
                    (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == X509KeyStorageFlags.PersistKeySet;

                keychain = persist
                    ? Interop.AppleCrypto.SecKeychainCopyDefault()
                    : Interop.AppleCrypto.CreateTemporaryKeychain();
            }
            else
            {
                keychain = SafeTemporaryKeychainHandle.InvalidHandle;
                password = SafePasswordHandle.InvalidHandle;
            }

            // Only dispose tmpKeychain on the exception path, otherwise it's managed by AppleCertLoader.
            try
            {
                SafeCFArrayHandle certs = Interop.AppleCrypto.X509ImportCollection(
                    rawData,
                    contentType,
                    password,
                    keychain,
                    exportable);

                // If the default keychain was used, null will be passed to the loader.
                return new AppleCertLoader(certs, keychain as SafeTemporaryKeychainHandle);
            }
            catch
            {
                keychain.Dispose();
                throw;
            }
        }

        public static ILoaderPal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            byte[] fileBytes = File.ReadAllBytes(fileName);
            return FromBlob(fileBytes, password, keyStorageFlags);
        }

        public static IExportPal FromCertificate(ICertificatePal cert)
        {
            return new AppleCertificateExporter(cert);
        }

        public static IExportPal LinkFromCertificateCollection(X509Certificate2Collection certificates)
        {
            return new AppleCertificateExporter(certificates);
        }

        public static IStorePal FromSystemStore(string storeName, StoreLocation storeLocation, OpenFlags openFlags)
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

                    break;
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
