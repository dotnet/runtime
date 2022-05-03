// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal static partial class CertificatePal
    {
        internal static partial ICertificatePal FromHandle(IntPtr handle)
        {
            return OpenSslX509CertificateReader.FromHandle(handle);
        }

        internal static partial ICertificatePal FromOtherCert(X509Certificate copyFrom)
        {
            return OpenSslX509CertificateReader.FromOtherCert(copyFrom);
        }

        internal static partial ICertificatePal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            return OpenSslX509CertificateReader.FromBlob(rawData, password, keyStorageFlags);
        }

        internal static partial ICertificatePal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            return OpenSslX509CertificateReader.FromFile(fileName, password, keyStorageFlags);
        }
    }
}
