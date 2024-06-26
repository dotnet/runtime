// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.X509Certificates
{
    public static partial class X509CertificateLoader
    {
        private static partial ICertificatePal LoadCertificatePal(ReadOnlySpan<byte> data)
        {
            throw new PlatformNotSupportedException(SR.SystemSecurityCryptographyX509Certificates_PlatformNotSupported);
        }

        private static partial ICertificatePal LoadCertificatePalFromFile(string path)
        {
            throw new PlatformNotSupportedException(SR.SystemSecurityCryptographyX509Certificates_PlatformNotSupported);
        }

        private static partial Pkcs12Return LoadPkcs12(
            ref BagState bagState,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags)
        {
            throw new PlatformNotSupportedException(SR.SystemSecurityCryptographyX509Certificates_PlatformNotSupported);
        }

        private static partial X509Certificate2Collection LoadPkcs12Collection(
            ref BagState bagState,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags)
        {
            throw new PlatformNotSupportedException(SR.SystemSecurityCryptographyX509Certificates_PlatformNotSupported);
        }
    }
}
