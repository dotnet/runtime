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
            X509KeyStorageFlags nonExportFlags,
            Action<X509Certificate2> otherWork)
        {
            X509KeyStorageFlags exportFlags = nonExportFlags | X509KeyStorageFlags.Exportable;

            ReadPfx(pfxBytes, correctPassword, expectedCert, null, otherWork, nonExportFlags);
            ReadPfx(pfxBytes, correctPassword, expectedCert, null, otherWork, exportFlags);
        }

        protected override void ReadMultiPfx(
            byte[] pfxBytes,
            string correctPassword,
            X509Certificate2 expectedSingleCert,
            X509Certificate2[] expectedOrder,
            X509KeyStorageFlags nonExportFlags,
            Action<X509Certificate2> perCertOtherWork)
        {
            ReadPfx(
                pfxBytes,
                correctPassword,
                expectedSingleCert,
                expectedOrder,
                perCertOtherWork,
                nonExportFlags);

            ReadPfx(
                pfxBytes,
                correctPassword,
                expectedSingleCert,
                expectedOrder,
                perCertOtherWork,
                nonExportFlags | X509KeyStorageFlags.Exportable);
        }

        private void ReadPfx(
            byte[] pfxBytes,
            string correctPassword,
            X509Certificate2 expectedCert,
            X509Certificate2[] expectedOrder,
            Action<X509Certificate2> otherWork,
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
            X509KeyStorageFlags importFlags,
            int win32Error,
            int altWin32Error)
        {
            X509Certificate2Collection coll = new X509Certificate2Collection();

            CryptographicException ex = Assert.ThrowsAny<CryptographicException>(
                () => coll.Import(pfxBytes, bestPassword, importFlags));

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
