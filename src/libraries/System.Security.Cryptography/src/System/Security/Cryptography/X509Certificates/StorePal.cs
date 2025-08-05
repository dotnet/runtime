// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal partial class StorePal
    {
        internal static partial IStorePal FromHandle(IntPtr storeHandle);

        internal static partial ILoaderPal FromBlob(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password,
            X509KeyStorageFlags keyStorageFlags);

        internal static partial ILoaderPal FromFile(
            string fileName,
            SafePasswordHandle password,
            X509KeyStorageFlags keyStorageFlags);

        internal static partial IExportPal FromCertificate(ICertificatePalCore cert);

        internal static partial IExportPal LinkFromCertificateCollection(
            X509Certificate2Collection certificates);

        internal static partial IStorePal FromSystemStore(
            string storeName,
            StoreLocation storeLocation,
            OpenFlags openFlags);

        internal sealed class CollectionBasedLoader : ILoaderPal
        {
            private X509Certificate2Collection? _coll;

            internal CollectionBasedLoader(X509Certificate2Collection coll)
            {
                _coll = coll;
            }

            public void Dispose()
            {
                X509Certificate2Collection? coll = _coll;
                _coll = null;

                if (coll is not null)
                {
                    foreach (X509Certificate2 cert in coll)
                    {
                        cert.Dispose();
                    }
                }
            }

            public void MoveTo(X509Certificate2Collection collection)
            {
                Debug.Assert(_coll is not null);
                collection.AddRange(_coll);
                _coll = null;
            }
        }
    }
}
