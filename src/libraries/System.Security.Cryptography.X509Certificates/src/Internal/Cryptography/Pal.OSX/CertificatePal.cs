// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class CertificatePal
    {
        public static ICertificatePal FromHandle(IntPtr handle)
        {
            return FromHandle(handle, true);
        }

        internal static ICertificatePal FromHandle(IntPtr handle, bool throwOnFail)
        {
            return AppleCertificatePal.FromHandle(handle, throwOnFail);
        }

        public static ICertificatePal FromOtherCert(X509Certificate cert)
        {
            return AppleCertificatePal.FromOtherCert(cert);
        }

        public static ICertificatePal FromBlob(
            byte[] rawData,
            SafePasswordHandle password,
            X509KeyStorageFlags keyStorageFlags)
        {
            return AppleCertificatePal.FromBlob(rawData, password, keyStorageFlags);
        }

        public static ICertificatePal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            return AppleCertificatePal.FromFile(fileName, password, keyStorageFlags);
        }
    }
}
