// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public sealed class PfxFormatTests_SingleCert : PfxFormatTests
    {
        protected override void ReadPfx(
            byte[] pfxBytes,
            string correctPassword,
            X509Certificate2 expectedCert,
            X509KeyStorageFlags nonExportFlags,
            Action<X509Certificate2> otherWork)
        {
            X509KeyStorageFlags exportFlags = nonExportFlags | X509KeyStorageFlags.Exportable;

            ReadPfx(pfxBytes, correctPassword, expectedCert, otherWork, nonExportFlags);
            ReadPfx(pfxBytes, correctPassword, expectedCert, otherWork, exportFlags);
        }

        protected override void ReadMultiPfx(
            byte[] pfxBytes,
            string correctPassword,
            X509Certificate2 expectedSingleCert,
            X509Certificate2[] expectedOrder,
            X509KeyStorageFlags nonExportFlags,
            Action<X509Certificate2> perCertOtherWork)
        {
            X509KeyStorageFlags exportFlags = nonExportFlags | X509KeyStorageFlags.Exportable;

            ReadPfx(pfxBytes, correctPassword, expectedSingleCert, perCertOtherWork, nonExportFlags);
            ReadPfx(pfxBytes, correctPassword, expectedSingleCert, perCertOtherWork, exportFlags);
        }

        private void ReadPfx(
            byte[] pfxBytes,
            string correctPassword,
            X509Certificate2 expectedCert,
            Action<X509Certificate2> otherWork,
            X509KeyStorageFlags flags)
        {
            using (X509Certificate2 cert = new X509Certificate2(pfxBytes, correctPassword, flags))
            {
                AssertCertEquals(expectedCert, cert);
                otherWork?.Invoke(cert);
            }
        }

        protected override void ReadEmptyPfx(byte[] pfxBytes, string correctPassword)
        {
            CryptographicException ex = Assert.Throws<CryptographicException>(
                () => new X509Certificate2(pfxBytes, correctPassword, s_importFlags));

            AssertMessageContains("no certificates", ex);
        }

        protected override void ReadWrongPassword(byte[] pfxBytes, string wrongPassword)
        {
            CryptographicException ex = Assert.ThrowsAny<CryptographicException>(
                () => new X509Certificate2(pfxBytes, wrongPassword, s_importFlags));

            AssertMessageContains("password", ex);
            Assert.Equal(ErrorInvalidPasswordHResult, ex.HResult);
        }

        protected override void ReadUnreadablePfx(
            byte[] pfxBytes,
            string bestPassword,
            X509KeyStorageFlags importFlags,
            int win32Error,
            int altWin32Error)
        {
            CryptographicException ex = Assert.ThrowsAny<CryptographicException>(
                () => new X509Certificate2(pfxBytes, bestPassword, importFlags));

            if (OperatingSystem.IsWindows())
            {
                if (altWin32Error != 0 && ex.HResult != altWin32Error)
                {
                    Assert.Equal(win32Error, ex.HResult);
                }
            }
            else
            {
                Assert.NotNull(ex.InnerException);
            }
        }

        private static void CheckBadKeyset(X509Certificate2 cert)
        {
            CryptographicException ex = Assert.ThrowsAny<CryptographicException>(
                    () => cert.GetRSAPrivateKey());

            // NTE_BAD_KEYSET
            Assert.Equal(-2146893802, ex.HResult);
        }

        protected override void CheckMultiBoundKeyConsistency(X509Certificate2 cert)
        {
            if (PlatformDetection.IsWindows)
            {
                CheckBadKeyset(cert);
            }
            else
            {
                base.CheckMultiBoundKeyConsistency(cert);
            }
        }

        protected override void CheckMultiBoundKeyConsistencyFails(X509Certificate2 cert)
        {
            if (PlatformDetection.IsWindows)
            {
                CheckBadKeyset(cert);
            }
            else
            {
                base.CheckMultiBoundKeyConsistencyFails(cert);
            }
        }
    }
}
