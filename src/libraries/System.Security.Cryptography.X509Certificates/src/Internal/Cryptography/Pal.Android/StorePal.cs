// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal
    {
        public static IStorePal FromHandle(IntPtr storeHandle)
        {
            throw new NotImplementedException($"{nameof(StorePal)}.{nameof(FromHandle)}");
        }

        public static ILoaderPal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            X509ContentType contentType = X509Certificate2.GetCertContentType(rawData);
            if (contentType == X509ContentType.Pkcs12)
            {
                ICertificatePal[] certPals = ReadPkcs12Collection(rawData, password);
                return new AndroidCertLoader(certPals);
            }
            else
            {
                SafeX509Handle[] certs = Interop.AndroidCrypto.X509DecodeCollection(rawData);
                return new AndroidCertLoader(certs);
            }
        }

        public static ILoaderPal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            byte[] fileBytes = File.ReadAllBytes(fileName);
            return FromBlob(fileBytes, password, keyStorageFlags);
        }

        public static IExportPal FromCertificate(ICertificatePalCore cert)
        {
            return new AndroidExportProvider(cert);
        }

        public static IExportPal LinkFromCertificateCollection(X509Certificate2Collection certificates)
        {
            return new AndroidExportProvider(certificates);
        }

        public static IStorePal FromSystemStore(string storeName, StoreLocation storeLocation, OpenFlags openFlags)
        {
            throw new NotImplementedException(nameof(FromSystemStore));
        }

        private static ICertificatePal[] ReadPkcs12Collection(ReadOnlySpan<byte> rawData, SafePasswordHandle password)
        {
            using (var reader = new AndroidPkcs12Reader(rawData))
            {
                reader.Decrypt(password);

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
