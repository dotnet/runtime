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
            Action<X509Certificate2> otherWork,
            X509KeyStorageFlags? requiredFlags)
        {
            ReadPfx(pfxBytes, correctPassword, expectedCert, otherWork, null, requiredFlags ?? s_importFlags);

            if (requiredFlags is null)
            {
                ReadPfx(pfxBytes, correctPassword, expectedCert, otherWork, null, s_exportableImportFlags);
            }
        }

        protected override void ReadMultiPfx(
            byte[] pfxBytes,
            string correctPassword,
            X509Certificate2 expectedSingleCert,
            X509Certificate2[] expectedOrder,
            Action<X509Certificate2> perCertOtherWork,
            Action<X509Certificate2Collection> collectionWork,
            X509KeyStorageFlags? requiredFlags)
        {
            ReadPfx(
                pfxBytes,
                correctPassword,
                expectedSingleCert,
                perCertOtherWork,
                requiredFlags ?? s_importFlags);

            if (requiredFlags is null)
            {
                ReadPfx(
                    pfxBytes,
                    correctPassword,
                    expectedSingleCert,
                    perCertOtherWork,
                    s_exportableImportFlags);
            }
        }

        private void ReadPfx(
            byte[] pfxBytes,
            string correctPassword,
            X509Certificate2 expectedCert,
            Action<X509Certificate2> otherWork,
            Action<X509Certificate2Collection> collectionWork,
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
            int win32Error,
            int altWin32Error,
            X509KeyStorageFlags? requiredFlags)
        {
            CryptographicException ex = Assert.ThrowsAny<CryptographicException>(
                () => new X509Certificate2(pfxBytes, bestPassword, requiredFlags ?? s_importFlags));

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
    }
}
