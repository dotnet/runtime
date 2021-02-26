// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class CertificatePal
    {
        public static ICertificatePal FromHandle(IntPtr handle)
        {
            return AndroidCertificatePal.FromHandle(handle);
        }

        public static ICertificatePal FromOtherCert(X509Certificate cert)
        {
            return AndroidCertificatePal.FromOtherCert(cert);
        }

        public static ICertificatePal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            return AndroidCertificatePal.FromBlob(rawData, password, keyStorageFlags);
        }

        public static ICertificatePal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            return AndroidCertificatePal.FromFile(fileName, password, keyStorageFlags);
        }
    }
}
