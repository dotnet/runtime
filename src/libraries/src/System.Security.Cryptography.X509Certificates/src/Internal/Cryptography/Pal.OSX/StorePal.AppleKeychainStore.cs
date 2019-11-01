// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal
    {
        private sealed class AppleKeychainStore : IStorePal
        {
            private SafeKeychainHandle _keychainHandle;
            private readonly bool _readonly;

            internal AppleKeychainStore(SafeKeychainHandle keychainHandle, OpenFlags openFlags)
            {
                Debug.Assert(keychainHandle != null && !keychainHandle.IsInvalid);

                _keychainHandle = keychainHandle;

                _readonly = (openFlags & (OpenFlags.ReadWrite | OpenFlags.MaxAllowed)) == 0;
            }

            public void Dispose()
            {
                _keychainHandle?.Dispose();
                _keychainHandle = null;
            }

            public void CloneTo(X509Certificate2Collection collection)
            {
                HashSet<X509Certificate2> dedupedCerts = new HashSet<X509Certificate2>();

                using (SafeCFArrayHandle identities = Interop.AppleCrypto.KeychainEnumerateIdentities(_keychainHandle))
                {
                    ReadCollection(identities, dedupedCerts);
                }

                using (SafeCFArrayHandle certs = Interop.AppleCrypto.KeychainEnumerateCerts(_keychainHandle))
                {
                    ReadCollection(certs, dedupedCerts);
                }

                foreach (X509Certificate2 cert in dedupedCerts)
                {
                    collection.Add(cert);
                }
            }

            public void Add(ICertificatePal cert)
            {
                if (_readonly)
                    throw new CryptographicException(SR.Cryptography_X509_StoreReadOnly);

                AppleCertificatePal applePal = (AppleCertificatePal)cert;

                var handle = (SafeKeychainItemHandle)applePal.IdentityHandle ?? applePal.CertificateHandle;
                Interop.AppleCrypto.X509StoreAddCertificate(handle, _keychainHandle);
            }

            public void Remove(ICertificatePal cert)
            {
                AppleCertificatePal applePal = (AppleCertificatePal)cert;

                var handle = (SafeKeychainItemHandle)applePal.IdentityHandle ?? applePal.CertificateHandle;
                Interop.AppleCrypto.X509StoreRemoveCertificate(handle, _keychainHandle, _readonly);
            }

            public SafeHandle SafeHandle => _keychainHandle;

            public static AppleKeychainStore OpenDefaultKeychain(OpenFlags openFlags)
            {
                return new AppleKeychainStore(Interop.AppleCrypto.SecKeychainCopyDefault(), openFlags);
            }

            public static AppleKeychainStore OpenSystemSharedKeychain(OpenFlags openFlags)
            {
                const string SharedSystemKeychainPath = "/Library/Keychains/System.keychain";
                return OpenKeychain(SharedSystemKeychainPath, openFlags);
            }

            public static AppleKeychainStore CreateOrOpenKeychain(string keychainPath, OpenFlags openFlags)
            {
                return new AppleKeychainStore(Interop.AppleCrypto.CreateOrOpenKeychain(keychainPath, !openFlags.HasFlag(OpenFlags.OpenExistingOnly)), openFlags);
            }

            private static AppleKeychainStore OpenKeychain(string keychainPath, OpenFlags openFlags)
            {
                return new AppleKeychainStore(Interop.AppleCrypto.SecKeychainOpen(keychainPath), openFlags);
            }
        }
    }
}
