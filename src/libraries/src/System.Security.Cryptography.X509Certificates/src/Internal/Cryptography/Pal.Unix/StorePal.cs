// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal
    {
        public static IStorePal FromBlob(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags)
        {
            throw new NotImplementedException();
        }

        public static IStorePal FromFile(string fileName, string password, X509KeyStorageFlags keyStorageFlags)
        {
            throw new NotImplementedException();
        }

        public static IStorePal FromCertificate(ICertificatePal cert)
        {
            throw new NotImplementedException();
        }

        public static IStorePal LinkFromCertificateCollection(X509Certificate2Collection certificates)
        {
            throw new NotImplementedException();
        }

        public static IStorePal FromSystemStore(string storeName, StoreLocation storeLocation, OpenFlags openFlags)
        {
            throw new NotImplementedException();
        }
    }
}
