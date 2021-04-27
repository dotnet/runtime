// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class ExportTests
    {
        [Fact]
        public static void ExportAsCert_CreatesCopy()
        {
            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificate))
            {
                byte[] first = cert.Export(X509ContentType.Cert);
                byte[] second = cert.Export(X509ContentType.Cert);
                Assert.NotSame(first, second);
            }
        }

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
            const string password = "PLACEHOLDER";

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
            const string password = "PLACEHOLDER";

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
                const string password = "PLACEHOLDER";

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

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [OuterLoop("Modifies user-persisted state")]
        public static void ExportDoesNotCorruptPrivateKeyMethods()
        {
            string keyName = $"clrtest.{Guid.NewGuid():D}";
            X509Store cuMy = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            cuMy.Open(OpenFlags.ReadWrite);
            X509Certificate2 createdCert = null;
            X509Certificate2 foundCert = null;
            X509Certificate2 foundCert2 = null;

            try
            {
                string commonName = nameof(ExportDoesNotCorruptPrivateKeyMethods);
                string subject = $"CN={commonName},OU=.NET";

                using (ImportedCollection toClean = new ImportedCollection(cuMy.Certificates))
                {
                    X509Certificate2Collection coll = toClean.Collection;

                    using (ImportedCollection matches =
                        new ImportedCollection(coll.Find(X509FindType.FindBySubjectName, commonName, false)))
                    {
                        foreach (X509Certificate2 cert in matches.Collection)
                        {
                            cuMy.Remove(cert);
                        }
                    }
                }

                foreach (X509Certificate2 cert in cuMy.Certificates)
                {
                    if (subject.Equals(cert.Subject))
                    {
                        cuMy.Remove(cert);
                    }

                    cert.Dispose();
                }

                CngKeyCreationParameters options = new CngKeyCreationParameters
                {
                    ExportPolicy = CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport,
                };

                using (CngKey key = CngKey.Create(CngAlgorithm.Rsa, keyName, options))
                using (RSACng rsaCng = new RSACng(key))
                {
                    CertificateRequest certReq = new CertificateRequest(
                        subject,
                        rsaCng,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);

                    DateTimeOffset now = DateTimeOffset.UtcNow.AddMinutes(-5);
                    createdCert = certReq.CreateSelfSigned(now, now.AddDays(1));
                }

                cuMy.Add(createdCert);

                using (ImportedCollection toClean = new ImportedCollection(cuMy.Certificates))
                {
                    X509Certificate2Collection matches = toClean.Collection.Find(
                        X509FindType.FindBySubjectName,
                        commonName,
                        validOnly: false);

                    Assert.Equal(1, matches.Count);
                    foundCert = matches[0];
                }

                Assert.False(HasEphemeralKey(foundCert));
                foundCert.Export(X509ContentType.Pfx, "");
                Assert.False(HasEphemeralKey(foundCert));

                using (ImportedCollection toClean = new ImportedCollection(cuMy.Certificates))
                {
                    X509Certificate2Collection matches = toClean.Collection.Find(
                        X509FindType.FindBySubjectName,
                        commonName,
                        validOnly: false);

                    Assert.Equal(1, matches.Count);
                    foundCert2 = matches[0];
                }

                Assert.False(HasEphemeralKey(foundCert2));
            }
            finally
            {
                if (createdCert != null)
                {
                    cuMy.Remove(createdCert);
                    createdCert.Dispose();
                }

                cuMy.Dispose();

                foundCert?.Dispose();
                foundCert2?.Dispose();

                try
                {
                    CngKey key = CngKey.Open(keyName);
                    key.Delete();
                    key.Dispose();
                }
                catch (Exception)
                {
                }
            }

            bool HasEphemeralKey(X509Certificate2 c)
            {
                using (RSA key = c.GetRSAPrivateKey())
                {
                    // This code is not defensive against the type changing, because it
                    // is in the source tree with the code that produces the value.
                    // Don't blind-cast like this in library or application code.
                    RSACng rsaCng = (RSACng)key;
                    return rsaCng.Key.IsEphemeral;
                }
            }
        }
    }
}
