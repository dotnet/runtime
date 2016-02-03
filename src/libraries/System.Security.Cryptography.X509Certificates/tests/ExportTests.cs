// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class ExportTests
    {
        [Fact]
        public static void ExportAsCert()
        {
            using (X509Certificate2 c1 = new X509Certificate2(TestData.MsCertificate))
            {
                byte[] rawData = c1.Export(X509ContentType.Cert);
                Assert.Equal(X509ContentType.Cert, X509Certificate2.GetCertContentType(rawData));
                Assert.Equal(TestData.MsCertificate, rawData);
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public static void ExportAsSerializedCert_Windows()
        {
            using (X509Certificate2 c1 = new X509Certificate2(TestData.MsCertificate))
            {
                byte[] serializedCert = c1.Export(X509ContentType.SerializedCert);

                Assert.Equal(X509ContentType.SerializedCert, X509Certificate2.GetCertContentType(serializedCert));

                using (X509Certificate2 c2 = new X509Certificate2(serializedCert))
                {
                    byte[] rawData = c2.Export(X509ContentType.Cert);
                    Assert.Equal(TestData.MsCertificate, rawData);
                }
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public static void ExportAsSerializedCert_Unix()
        {
            using (X509Certificate2 c1 = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.Throws<PlatformNotSupportedException>(() => c1.Export(X509ContentType.SerializedCert));
            }
        }

        [Fact]
        public static void ExportAsPfx()
        {
            using (X509Certificate2 c1 = new X509Certificate2(TestData.MsCertificate))
            {
                byte[] pfx = c1.Export(X509ContentType.Pkcs12);
                Assert.Equal(X509ContentType.Pkcs12, X509Certificate2.GetCertContentType(pfx));

                using (X509Certificate2 c2 = new X509Certificate2(pfx))
                {
                    byte[] rawData = c2.Export(X509ContentType.Cert);
                    Assert.Equal(TestData.MsCertificate, rawData);
                }
            }
        }

        [Fact]
        public static void ExportAsPfxWithPassword()
        {
            const string password = "Cotton";

            using (X509Certificate2 c1 = new X509Certificate2(TestData.MsCertificate))
            {
                byte[] pfx = c1.Export(X509ContentType.Pkcs12, password);
                Assert.Equal(X509ContentType.Pkcs12, X509Certificate2.GetCertContentType(pfx));

                using (X509Certificate2 c2 = new X509Certificate2(pfx, password))
                {
                    byte[] rawData = c2.Export(X509ContentType.Cert);
                    Assert.Equal(TestData.MsCertificate, rawData);
                }
            }
        }

        [Fact]
        public static void ExportAsPfxVerifyPassword()
        {
            const string password = "Cotton";

            using (X509Certificate2 c1 = new X509Certificate2(TestData.MsCertificate))
            {
                byte[] pfx = c1.Export(X509ContentType.Pkcs12, password);
                Assert.ThrowsAny<CryptographicException>(() => new X509Certificate2(pfx, "WRONGPASSWORD"));
            }
        }
    }
}
