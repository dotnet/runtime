// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class CertificatePal
    {
        public static ICertificatePal FromHandle(IntPtr handle)
        {
            return OpenSslX509CertificateReader.FromHandle(handle);
        }

        public static ICertificatePal FromOtherCert(X509Certificate cert)
        {
            return OpenSslX509CertificateReader.FromOtherCert(cert);
        }

        public static ICertificatePal FromBlob(byte[] rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            return OpenSslX509CertificateReader.FromBlob(rawData, password, keyStorageFlags);
        }

        public static ICertificatePal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            return OpenSslX509CertificateReader.FromFile(fileName, password, keyStorageFlags);
        }
    }
}
