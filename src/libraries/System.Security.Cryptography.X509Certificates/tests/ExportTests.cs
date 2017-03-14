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
        [PlatformSpecific(TestPlatforms.Windows)]  // SerializedCert not supported on Unix
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
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // SerializedCert not supported on Unix
        public static void ExportAsSerializedCert_Unix()
        {
            using (X509Certificate2 c1 = new X509Certificate2(TestData.MsCertificate))
            {
                Assert.Throws<PlatformNotSupportedException>(() => c1.Export(X509ContentType.SerializedCert));
            }
        }

        [Fact]
        [ActiveIssue(16705, TestPlatforms.OSX)]
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
        [ActiveIssue(16705, TestPlatforms.OSX)]
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
        [ActiveIssue(16705, TestPlatforms.OSX)]
        public static void ExportAsPfxVerifyPassword()
        {
            const string password = "Cotton";

            using (X509Certificate2 c1 = new X509Certificate2(TestData.MsCertificate))
            {
                byte[] pfx = c1.Export(X509ContentType.Pkcs12, password);
                Assert.ThrowsAny<CryptographicException>(() => new X509Certificate2(pfx, "WRONGPASSWORD"));
            }
        }

        [Fact]
        public static void ExportAsPfxWithPrivateKeyVerifyPassword()
        {
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            {
                Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");

                const string password = "Cotton";

                byte[] pfx = cert.Export(X509ContentType.Pkcs12, password);

                Assert.ThrowsAny<CryptographicException>(() => new X509Certificate2(pfx, "WRONGPASSWORD"));

                using (var cert2 = new X509Certificate2(pfx, password))
                {
                    Assert.Equal(cert, cert2);
                    Assert.True(cert2.HasPrivateKey, "cert2.HasPrivateKey");
                }
            }
        }

        [Fact]
        public static void ExportAsPfxWithPrivateKey()
        {
            using (X509Certificate2 cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            {
                Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");

                byte[] pfxBytes = cert.Export(X509ContentType.Pkcs12);

                using (X509Certificate2 fromPfx = new X509Certificate2(pfxBytes))
                {
                    Assert.Equal(cert, fromPfx);
                    Assert.True(fromPfx.HasPrivateKey, "fromPfx.HasPrivateKey");

                    byte[] origSign;
                    byte[] copySign;

                    using (RSA origPriv = cert.GetRSAPrivateKey())
                    {
                        origSign = origPriv.SignData(pfxBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    }

                    using (RSA copyPriv = fromPfx.GetRSAPrivateKey())
                    {
                        copySign = copyPriv.SignData(pfxBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    }

                    using (RSA origPub = cert.GetRSAPublicKey())
                    {
                        Assert.True(
                            origPub.VerifyData(pfxBytes, copySign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                            "oPub v copySig");
                    }

                    using (RSA copyPub = fromPfx.GetRSAPublicKey())
                    {
                        Assert.True(
                            copyPub.VerifyData(pfxBytes, origSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                            "copyPub v oSig");
                    }
                }
            }
        }
    }
}
