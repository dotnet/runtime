// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            private readonly bool _readonly;

            internal AppleKeychainStore(OpenFlags openFlags)
            {
                _readonly = (openFlags & (OpenFlags.ReadWrite | OpenFlags.MaxAllowed)) == 0;
            }

            public void Dispose()
            {
            }

            public void CloneTo(X509Certificate2Collection collection)
            {
                HashSet<X509Certificate2> dedupedCerts = new HashSet<X509Certificate2>();

                using (SafeCFArrayHandle identities = Interop.AppleCrypto.KeychainEnumerateIdentities())
                {
                    ReadCollection(identities, dedupedCerts);
                }

                using (SafeCFArrayHandle certs = Interop.AppleCrypto.KeychainEnumerateCerts())
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

                var handle = (SafeHandle?)applePal.IdentityHandle ?? applePal.CertificateHandle;
                Interop.AppleCrypto.X509StoreAddCertificate(handle);
            }

            public void Remove(ICertificatePal cert)
            {
                AppleCertificatePal applePal = (AppleCertificatePal)cert;

                var handle = (SafeHandle?)applePal.IdentityHandle ?? applePal.CertificateHandle;
                Interop.AppleCrypto.X509StoreRemoveCertificate(handle, _readonly);
            }

            public SafeHandle? SafeHandle { get; }

            public static AppleKeychainStore OpenDefaultKeychain(OpenFlags openFlags)
            {
                return new AppleKeychainStore(openFlags);
            }
        }
    }
}
