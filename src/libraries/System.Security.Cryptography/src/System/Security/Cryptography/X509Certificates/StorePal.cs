// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    }
}
