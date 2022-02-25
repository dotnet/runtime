// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal static partial class CertificatePal
    {
        internal static partial ICertificatePal FromHandle(IntPtr handle)
        {
            return AppleCertificatePal.FromHandle(handle, true)!;
        }

        internal static partial ICertificatePal FromOtherCert(X509Certificate copyFrom)
        {
            return AppleCertificatePal.FromOtherCert(copyFrom);
        }

        internal static partial ICertificatePal FromBlob(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password,
            X509KeyStorageFlags keyStorageFlags)
        {
            return AppleCertificatePal.FromBlob(rawData, password, keyStorageFlags);
        }

        internal static partial ICertificatePal FromFile(
            string fileName,
            SafePasswordHandle password,
            X509KeyStorageFlags keyStorageFlags)
        {
            return AppleCertificatePal.FromFile(fileName, password, keyStorageFlags);
        }
    }
}
