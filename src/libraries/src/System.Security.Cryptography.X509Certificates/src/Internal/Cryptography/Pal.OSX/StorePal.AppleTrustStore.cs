// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal
    {
        private sealed class AppleTrustStore : IStorePal
        {
            private readonly StoreName _storeName;
            private readonly StoreLocation _location;

            private AppleTrustStore(StoreName storeName, StoreLocation location)
            {
                Debug.Assert(storeName == StoreName.Root || storeName == StoreName.Disallowed);

                _storeName = storeName;
                _location = location;
            }

            public void Dispose()
            {
                // Nothing to do.
            }

            public void CloneTo(X509Certificate2Collection collection)
            {
                HashSet<X509Certificate2> dedupedCerts = new HashSet<X509Certificate2>();

                if (_storeName == StoreName.Root)
                {
                    using (SafeCFArrayHandle certs = Interop.AppleCrypto.StoreEnumerateRoot(_location))
                    {
                        ReadCollection(certs, dedupedCerts);
                    }
                }
                else if (_storeName == StoreName.Disallowed)
                {
                    using (SafeCFArrayHandle certs = Interop.AppleCrypto.StoreEnumerateDisallowed(_location))
                    {
                        ReadCollection(certs, dedupedCerts);
                    }
                }
                else
                {
                    Debug.Fail($"No handler for trust store {_storeName}");
                }

                foreach (X509Certificate2 cert in dedupedCerts)
                {
                    collection.Add(cert);
                }
            }

            public void Add(ICertificatePal cert)
            {
                throw new CryptographicException(SR.Cryptography_X509_StoreReadOnly);
            }

            public void Remove(ICertificatePal cert)
            {
                throw new CryptographicException(SR.Cryptography_X509_StoreReadOnly);
            }

            public SafeHandle SafeHandle => null;

            internal static AppleTrustStore OpenStore(StoreName storeName, StoreLocation location, OpenFlags openFlags)
            {
                if ((openFlags & OpenFlags.ReadWrite) == OpenFlags.ReadWrite)
                    throw new CryptographicException(SR.Security_AccessDenied);

                return new AppleTrustStore(storeName, location);
            }
        }
    }
}
