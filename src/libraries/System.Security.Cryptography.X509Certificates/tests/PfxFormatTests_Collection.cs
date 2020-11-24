// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public sealed class PfxFormatTests_Collection : PfxFormatTests
    {
        protected override void ReadPfx(
            byte[] pfxBytes,
            string correctPassword,
            X509Certificate2 expectedCert,
            Action<X509Certificate2> otherWork,
            X509KeyStorageFlags? requiredFlags)
        {
            ReadPfx(pfxBytes, correctPassword, expectedCert, null, otherWork, null, requiredFlags ?? s_importFlags);

            if (requiredFlags is null)
            {
                ReadPfx(pfxBytes, correctPassword, expectedCert, null, otherWork, null, s_exportableImportFlags);
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
                expectedOrder,
                perCertOtherWork,
                collectionWork,
                requiredFlags ?? s_importFlags);

            if (requiredFlags is null)
            {
                ReadPfx(
                    pfxBytes,
                    correctPassword,
                    expectedSingleCert,
                    expectedOrder,
                    perCertOtherWork,
                    collectionWork,
                    s_exportableImportFlags);
            }
        }

        private void ReadPfx(
            byte[] pfxBytes,
            string correctPassword,
            X509Certificate2 expectedCert,
            X509Certificate2[] expectedOrder,
            Action<X509Certificate2> otherWork,
            Action<X509Certificate2Collection> collectionWork,
            X509KeyStorageFlags flags)
        {
            using (ImportedCollection imported = Cert.Import(pfxBytes, correctPassword, flags))
            {
                X509Certificate2Collection coll = imported.Collection;
                Assert.Equal(expectedOrder?.Length ?? 1, coll.Count);

                Span<X509Certificate2> testOrder = expectedOrder == null ?
                    MemoryMarshal.CreateSpan(ref expectedCert, 1) :
                    expectedOrder.AsSpan();

                for (int i = 0; i < testOrder.Length; i++)
                {
                    X509Certificate2 actual = coll[i];
                    AssertCertEquals(testOrder[i], actual);
                    otherWork?.Invoke(actual);
                }

                collectionWork?.Invoke(coll);
            }
        }

        protected override void ReadEmptyPfx(byte[] pfxBytes, string correctPassword)
        {
            X509Certificate2Collection coll = new X509Certificate2Collection();
            coll.Import(pfxBytes, correctPassword, s_importFlags);
            Assert.Equal(0, coll.Count);
        }

        protected override void ReadWrongPassword(byte[] pfxBytes, string wrongPassword)
        {
            X509Certificate2Collection coll = new X509Certificate2Collection();

            CryptographicException ex = Assert.ThrowsAny<CryptographicException>(
                () => coll.Import(pfxBytes, wrongPassword, s_importFlags));

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
            X509Certificate2Collection coll = new X509Certificate2Collection();

            CryptographicException ex = Assert.ThrowsAny<CryptographicException>(
                () => coll.Import(pfxBytes, bestPassword, requiredFlags ?? s_importFlags));

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
