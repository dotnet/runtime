// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class CertificatePal
    {
        public static ICertificatePal FromHandle(IntPtr handle)
        {
            throw new NotImplementedException();
        }

        public static ICertificatePal FromBlob(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags)
        {
            return new OpenSslX509CertificateReader(rawData);
        }

        public static ICertificatePal FromFile(string fileName, string password, X509KeyStorageFlags keyStorageFlags)
        {
            throw new NotImplementedException();
        }
    }
}
