// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography.Dsa.Tests;
using System.Security.Cryptography.EcDsa.Tests;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs7;
using System.Security.Cryptography.Asn1.Pkcs12;
using System.Security.Cryptography.Pkcs;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support X.509 certificates")]
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

                using (X509Certificate2 c2 = new X509Certificate2(pfx, (string?)null))
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
                Assert.ThrowsAny<CryptographicException>(() => new X509Certificate2(pfx, "PlaceholderWrongPassword"));
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        public static void ExportAsPfxWithPrivateKeyVerifyPassword()
        {
            using (var cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            {
                Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");
                const string password = "PLACEHOLDER";

                byte[] pfx = cert.Export(X509ContentType.Pkcs12, password);

                Assert.ThrowsAny<CryptographicException>(() => new X509Certificate2(pfx, "PlaceholderWrongPassword"));

                using (var cert2 = new X509Certificate2(pfx, password))
                {
                    Assert.Equal(cert, cert2);
                    Assert.True(cert2.HasPrivateKey, "cert2.HasPrivateKey");
                }
            }
        }

        [Theory]
        [InlineData(Pkcs12ExportPbeParameters.Pkcs12TripleDesSha1, nameof(HashAlgorithmName.SHA1), PbeEncryptionAlgorithm.TripleDes3KeyPkcs12)]
        [InlineData(Pkcs12ExportPbeParameters.Pbes2Aes256Sha256, nameof(HashAlgorithmName.SHA256), PbeEncryptionAlgorithm.Aes256Cbc)]
        [InlineData(Pkcs12ExportPbeParameters.Default, nameof(HashAlgorithmName.SHA256), PbeEncryptionAlgorithm.Aes256Cbc)]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        public static void ExportPkcs12(
            Pkcs12ExportPbeParameters pkcs12ExportPbeParameters,
            string expectedHashAlgorithm,
            PbeEncryptionAlgorithm expectedEncryptionAlgorithm)
        {
            const string password = "PLACEHOLDER";

            using (X509Certificate2 cert = new(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            {
                byte[] pkcs12 = cert.ExportPkcs12(pkcs12ExportPbeParameters, password);
                (int certs, int keys) = VerifyPkcs12(
                    pkcs12,
                    password,
                    expectedIterations: 2000,
                    expectedMacHashAlgorithm: new HashAlgorithmName(expectedHashAlgorithm),
                    expectedEncryptionAlgorithm);
                Assert.Equal(1, certs);
                Assert.Equal(1, keys);
            }
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        [InlineData(PbeEncryptionAlgorithm.Aes192Cbc, nameof(HashAlgorithmName.SHA1), 1200)]
        [InlineData(PbeEncryptionAlgorithm.Aes256Cbc, nameof(HashAlgorithmName.SHA256), 4000)]
        [InlineData(PbeEncryptionAlgorithm.Aes128Cbc, nameof(HashAlgorithmName.SHA256), 4)]
        [InlineData(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, nameof(HashAlgorithmName.SHA1), 1234)]
        public static void ExportPkcs12_PbeParameters(
            PbeEncryptionAlgorithm encryptionAlgorithm,
            string hashAlgorithm,
            int iterations)
        {
            const string password = "PLACEHOLDER";
            HashAlgorithmName hashAlgorithmName = new(hashAlgorithm);
            PbeParameters parameters = new(encryptionAlgorithm, hashAlgorithmName, iterations);

            using (X509Certificate2 cert = new(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            {
                byte[] pkcs12 = cert.ExportPkcs12(parameters, password);
                (int certs, int keys) = VerifyPkcs12(
                    pkcs12,
                    password,
                    iterations,
                    hashAlgorithmName,
                    encryptionAlgorithm);
                Assert.Equal(1, certs);
                Assert.Equal(1, keys);
            }
        }

        [Theory]
        [InlineData(PbeEncryptionAlgorithm.Aes192Cbc, nameof(HashAlgorithmName.SHA1), 1200)]
        [InlineData(PbeEncryptionAlgorithm.Aes256Cbc, nameof(HashAlgorithmName.SHA256), 4000)]
        [InlineData(PbeEncryptionAlgorithm.Aes128Cbc, nameof(HashAlgorithmName.SHA256), 4)]
        [InlineData(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, nameof(HashAlgorithmName.SHA1), 1234)]
        public static void ExportPkcs12_PbeParameters_CertOnly(
            PbeEncryptionAlgorithm encryptionAlgorithm,
            string hashAlgorithm,
            int iterations)
        {
            const string password = "PLACEHOLDER";
            HashAlgorithmName hashAlgorithmName = new(hashAlgorithm);
            PbeParameters parameters = new(encryptionAlgorithm, hashAlgorithmName, iterations);

            using (X509Certificate2 cert = new(TestData.MsCertificate))
            {
                byte[] pkcs12 = cert.ExportPkcs12(parameters, password);
                (int certs, int keys) = VerifyPkcs12(
                    pkcs12,
                    password,
                    iterations,
                    hashAlgorithmName,
                    encryptionAlgorithm);
                Assert.Equal(1, certs);
                Assert.Equal(0, keys);
            }
        }

        [Theory]
        [InlineData(Pkcs12ExportPbeParameters.Pkcs12TripleDesSha1, nameof(HashAlgorithmName.SHA1), PbeEncryptionAlgorithm.TripleDes3KeyPkcs12)]
        [InlineData(Pkcs12ExportPbeParameters.Pbes2Aes256Sha256, nameof(HashAlgorithmName.SHA256), PbeEncryptionAlgorithm.Aes256Cbc)]
        [InlineData(Pkcs12ExportPbeParameters.Default, nameof(HashAlgorithmName.SHA256), PbeEncryptionAlgorithm.Aes256Cbc)]
        public static void ExportPkcs12_CertOnly(
            Pkcs12ExportPbeParameters pkcs12ExportPbeParameters,
            string expectedHashAlgorithm,
            PbeEncryptionAlgorithm expectedEncryptionAlgorithm)
        {
            const string password = "PLACEHOLDER";

            using (X509Certificate2 cert = new(TestData.MsCertificate))
            {
                byte[] pkcs12 = cert.ExportPkcs12(pkcs12ExportPbeParameters, password);
                (int certs, int keys) = VerifyPkcs12(
                    pkcs12,
                    password,
                    expectedIterations: 2000,
                    expectedMacHashAlgorithm: new HashAlgorithmName(expectedHashAlgorithm),
                    expectedEncryptionAlgorithm);
                Assert.Equal(1, certs);
                Assert.Equal(0, keys);
            }
        }

        [Fact]
        public static void ExportPkcs12_Pkcs12ExportPbeParameters_ArgValidation()
        {
            using (X509Certificate2 cert = new(TestData.PfxData, TestData.PfxDataPassword))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("exportParameters",
                    () => cert.ExportPkcs12((Pkcs12ExportPbeParameters)42, null));

                AssertExtensions.Throws<ArgumentException>("password",
                    () => cert.ExportPkcs12(Pkcs12ExportPbeParameters.Pbes2Aes256Sha256, "PLACE\0HOLDER"));
            }
        }

        [Theory]
        [InlineData(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, nameof(HashAlgorithmName.SHA256))]
        [InlineData(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, "")]
        [InlineData(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, null)]
        [InlineData(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, "POTATO")]
        [InlineData(PbeEncryptionAlgorithm.Aes128Cbc, "POTATO")]
        [InlineData(PbeEncryptionAlgorithm.Aes128Cbc, null)]
        [InlineData(PbeEncryptionAlgorithm.Aes128Cbc, "")]
        [InlineData(PbeEncryptionAlgorithm.Aes192Cbc, "POTATO")]
        [InlineData(PbeEncryptionAlgorithm.Aes192Cbc, null)]
        [InlineData(PbeEncryptionAlgorithm.Aes192Cbc, "")]
        [InlineData(PbeEncryptionAlgorithm.Aes256Cbc, "POTATO")]
        [InlineData(PbeEncryptionAlgorithm.Aes256Cbc, null)]
        [InlineData(PbeEncryptionAlgorithm.Aes256Cbc, "")]
        [InlineData(PbeEncryptionAlgorithm.Aes256Cbc, "SHA3-256")]
        [InlineData((PbeEncryptionAlgorithm)(-1), nameof(HashAlgorithmName.SHA1))]
        public static void ExportPkcs12_PbeParameters_ArgValidation(
            PbeEncryptionAlgorithm encryptionAlgorithm,
            string hashAlgorithm)
        {
            using (X509Certificate2 cert = new(TestData.PfxData, TestData.PfxDataPassword))
            {
                PbeParameters badParameters = new(encryptionAlgorithm, new HashAlgorithmName(hashAlgorithm), 1);
                Assert.Throws<CryptographicException>(() => cert.ExportPkcs12(badParameters, null));
            }
        }

        [Fact]
        public static void ExportPkcs12_PbeParameters_ArgValidation_Password()
        {
            using (X509Certificate2 cert = new(TestData.PfxData, TestData.PfxDataPassword))
            {
                PbeParameters parameters = new(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 1);
                AssertExtensions.Throws<ArgumentException>("password",
                    () => cert.ExportPkcs12(parameters, "PLACE\0HOLDER"));
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        public static void ExportAsPfxWithPrivateKey()
        {
            using (X509Certificate2 cert = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword, X509KeyStorageFlags.Exportable))
            {
                Assert.True(cert.HasPrivateKey, "cert.HasPrivateKey");

                byte[] pfxBytes = cert.Export(X509ContentType.Pkcs12);

                using (X509Certificate2 fromPfx = new X509Certificate2(pfxBytes, (string?)null))
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
        [OuterLoop("Modifies user-persisted state", ~TestPlatforms.Browser)]
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

        [Fact]
        public static void TryExportCertificatePem_DestinationTooSmall()
        {
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.CertRfc7468Wrapped))
            {
                // Too small by one
                Span<char> destination = new char[TestData.CertRfc7468Wrapped.Length - 1];
                Assert.False(cert.TryExportCertificatePem(destination, out int charsWritten));
                Assert.Equal(0, charsWritten);
            }
        }

        [Fact]
        public static void TryExportCertificatePem_DestinationJustRight()
        {
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.CertRfc7468Wrapped))
            {
                Span<char> destination = new char[TestData.CertRfc7468Wrapped.Length];
                Assert.True(cert.TryExportCertificatePem(destination, out int charsWritten));
                Assert.Equal(TestData.CertRfc7468Wrapped, new string(destination));
            }
        }

        [Fact]
        public static void TryExportCertificatePem_DestinationLarger()
        {
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.CertRfc7468Wrapped))
            {
                int padding = 10;
                Span<char> destination = new char[TestData.CertRfc7468Wrapped.Length + padding * 2];
                destination.Fill('!');
                Assert.True(cert.TryExportCertificatePem(destination.Slice(padding), out int charsWritten));
                Assert.Equal(TestData.CertRfc7468Wrapped, new string(destination.Slice(padding, charsWritten)));

                // Assert front padding is unaltered
                AssertExtensions.FilledWith('!', destination.Slice(0, padding));

                // Assert trailing padding is unaltered
                AssertExtensions.FilledWith('!', destination.Slice(charsWritten + padding));
            }
        }

        [Fact]
        public static void ExportCertificatePem()
        {
            using (X509Certificate2 cert = X509Certificate2.CreateFromPem(TestData.CertRfc7468Wrapped))
            {
                string pem = cert.ExportCertificatePem();
                Assert.Equal(TestData.CertRfc7468Wrapped, pem);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        public static void RSA_Export_DefaultKeyStorePermitsUnencryptedExports_ExportParameters()
        {
            (byte[] pkcs12, RSA rsa) = CreateSimplePkcs12<RSA>();

            using (rsa)
            {
                using X509Certificate2 cert = new X509Certificate2(pkcs12, "", X509KeyStorageFlags.Exportable);
                using RSA key = cert.GetRSAPrivateKey();
                RSAParameters expected = rsa.ExportParameters(true);
                RSAParameters actual = key.ExportParameters(true);

                Assert.Equal(expected.Modulus, actual.Modulus);
                Assert.Equal(expected.D, actual.D);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        public static void RSA_Export_DefaultKeyStorePermitsUnencryptedExports_Pkcs8PrivateKey()
        {
            (byte[] pkcs12, RSA rsa) = CreateSimplePkcs12<RSA>();

            using (rsa)
            {
                using X509Certificate2 cert = new X509Certificate2(pkcs12, "", X509KeyStorageFlags.Exportable);
                using RSA key = cert.GetRSAPrivateKey();
                byte[] exported = key.ExportPkcs8PrivateKey();

                using RSA imported = RSA.Create();
                imported.ImportPkcs8PrivateKey(exported, out _);
                RSAParameters actual = imported.ExportParameters(true);
                RSAParameters expected = rsa.ExportParameters(true);

                Assert.Equal(expected.Modulus, actual.Modulus);
                Assert.Equal(expected.D, actual.D);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        public static void ECDsa_Export_DefaultKeyStorePermitsUnencryptedExports_Pkcs8PrivateKey()
        {
            (byte[] pkcs12, ECDsa ecdsa) = CreateSimplePkcs12<ECDsa>();

            using (ecdsa)
            {
                using X509Certificate2 cert = new X509Certificate2(pkcs12, "", X509KeyStorageFlags.Exportable);
                using ECDsa key = cert.GetECDsaPrivateKey();
                byte[] exported = key.ExportPkcs8PrivateKey();

                using ECDsa imported = ECDsa.Create();
                imported.ImportPkcs8PrivateKey(exported, out _);
                ECParameters actual = imported.ExportParameters(true);
                ECParameters expected = ecdsa.ExportParameters(true);

                Assert.Equal(expected.D, actual.D);
                Assert.Equal(expected.Q.X, actual.Q.X);
                Assert.Equal(expected.Q.Y, actual.Q.Y);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        public static void ECDsa_Export_DefaultKeyStorePermitsUnencryptedExports_ExportParameters(bool explicitParameters)
        {
            if (explicitParameters && !ECDsaFactory.ExplicitCurvesSupported)
            {
                return;
            }

            (byte[] pkcs12, ECDsa ecdsa) = CreateSimplePkcs12<ECDsa>();

            using (ecdsa)
            {
                using X509Certificate2 cert = new X509Certificate2(pkcs12, "", X509KeyStorageFlags.Exportable);
                using ECDsa key = cert.GetECDsaPrivateKey();

                ECParameters actual = explicitParameters ? key.ExportExplicitParameters(true) : key.ExportParameters(true);
                ECParameters expected = explicitParameters ? ecdsa.ExportExplicitParameters(true) : ecdsa.ExportParameters(true);

                Assert.Equal(expected.D, actual.D);
                Assert.Equal(expected.Q.X, actual.Q.X);
                Assert.Equal(expected.Q.Y, actual.Q.Y);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        public static void ECDH_Export_DefaultKeyStorePermitsUnencryptedExports_ExportParameters(bool explicitParameters)
        {
            if (explicitParameters && !ECDsaFactory.ExplicitCurvesSupported)
            {
                return;
            }

            (byte[] pkcs12, ECDiffieHellman ecdh) = CreateSimplePkcs12<ECDiffieHellman>();

            using (ecdh)
            {
                using X509Certificate2 cert = new X509Certificate2(pkcs12, "", X509KeyStorageFlags.Exportable);
                using ECDiffieHellman key = cert.GetECDiffieHellmanPrivateKey();

                ECParameters actual = explicitParameters ? key.ExportExplicitParameters(true) : key.ExportParameters(true);
                ECParameters expected = explicitParameters ? ecdh.ExportExplicitParameters(true) : ecdh.ExportParameters(true);

                Assert.Equal(expected.D, actual.D);
                Assert.Equal(expected.Q.X, actual.Q.X);
                Assert.Equal(expected.Q.Y, actual.Q.Y);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        public static void ECDH_Export_DefaultKeyStorePermitsUnencryptedExports_Pkcs8PrivateKey()
        {
            (byte[] pkcs12, ECDiffieHellman ecdh) = CreateSimplePkcs12<ECDiffieHellman>();

            using (ecdh)
            {
                using X509Certificate2 cert = new X509Certificate2(pkcs12, "", X509KeyStorageFlags.Exportable);
                using ECDiffieHellman key = cert.GetECDiffieHellmanPrivateKey();
                byte[] exported = key.ExportPkcs8PrivateKey();

                using ECDiffieHellman imported = ECDiffieHellman.Create();
                imported.ImportPkcs8PrivateKey(exported, out _);
                ECParameters actual = imported.ExportParameters(true);
                ECParameters expected = ecdh.ExportParameters(true);

                Assert.Equal(expected.D, actual.D);
                Assert.Equal(expected.Q.X, actual.Q.X);
                Assert.Equal(expected.Q.Y, actual.Q.Y);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        public static void DSA_Export_DefaultKeyStorePermitsUnencryptedExports_ExportParameters()
        {
            (byte[] pkcs12, DSA dsa) = CreateSimplePkcs12<DSA>();

            using (dsa)
            {
                using X509Certificate2 cert = new X509Certificate2(pkcs12, "", X509KeyStorageFlags.Exportable);
                using DSA key = cert.GetDSAPrivateKey();
                DSAParameters expected = dsa.ExportParameters(true);
                DSAParameters actual = key.ExportParameters(true);

                Assert.Equal(expected.X, actual.X);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "The PKCS#12 Exportable flag is not supported on iOS/MacCatalyst/tvOS")]
        public static void DSA_Export_DefaultKeyStorePermitsUnencryptedExports_Pkcs8PrivateKey()
        {
            (byte[] pkcs12, DSA dsa) = CreateSimplePkcs12<DSA>();

            using (dsa)
            {
                using X509Certificate2 cert = new X509Certificate2(pkcs12, "", X509KeyStorageFlags.Exportable);
                using DSA key = cert.GetDSAPrivateKey();
                byte[] exported = key.ExportPkcs8PrivateKey();

                using DSA imported = DSA.Create();
                imported.ImportPkcs8PrivateKey(exported, out _);
                DSAParameters actual = imported.ExportParameters(true);
                DSAParameters expected = dsa.ExportParameters(true);

                Assert.Equal(expected.X, actual.X);
            }
        }

        private static (byte[] Pkcs12, TKey key) CreateSimplePkcs12<TKey>() where TKey : AsymmetricAlgorithm
        {
            using (ECDsa ca = ECDsa.Create(ECCurve.NamedCurves.nistP256))
            {
                CertificateRequest issuerRequest = new CertificateRequest(
                    new X500DistinguishedName("CN=root"),
                    ca,
                    HashAlgorithmName.SHA256);

                issuerRequest.CertificateExtensions.Add(X509BasicConstraintsExtension.CreateForCertificateAuthority());

                DateTimeOffset notBefore = DateTimeOffset.UtcNow;
                DateTimeOffset notAfter = notBefore.AddDays(30);
                byte[] serial = [1, 2, 3, 4, 5, 6, 7, 8];
                X509SignatureGenerator generator = X509SignatureGenerator.CreateForECDsa(ca);

                using (X509Certificate2 issuer = issuerRequest.CreateSelfSigned(notBefore, notAfter))
                {
                    CertificateRequest req;
                    TKey key;

                    if (typeof(TKey) == typeof(ECDsa))
                    {
                        ECDsa ecKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                        req = new("CN=simple", ecKey, HashAlgorithmName.SHA256);
                        key = (TKey)(object)ecKey;
                    }
                    else if (typeof(TKey) == typeof(RSA))
                    {
                        RSA rsaKey = RSA.Create(2048);
                        req = new("CN=simple", rsaKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                        key = (TKey)(object)rsaKey;
                    }
                    else if (typeof(TKey) == typeof(ECDiffieHellman))
                    {
                        ECDiffieHellman ecKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                        req = new CertificateRequest(new X500DistinguishedName("CN=simple"), new PublicKey(ecKey), HashAlgorithmName.SHA256);
                        req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyAgreement, true));
                        key = (TKey)(object)ecKey;
                    }
                    else if (typeof(TKey) == typeof(DSA))
                    {
                        DSA dsaKey = DSA.Create();
                        dsaKey.ImportParameters(DSATestData.GetDSA1024Params());
                        req = new CertificateRequest(new X500DistinguishedName("CN=simple"), new PublicKey(dsaKey), HashAlgorithmName.SHA256);
                        req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
                        key = (TKey)(object)dsaKey;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));

                    using X509Certificate2 cert = req.Create(issuer.SubjectName, generator, notBefore, notAfter, serial);
                    Pkcs9LocalKeyId keyId = new([1]);
                    PbeParameters pbe = new(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 1);

                    Pkcs12Builder builder = new();
                    Pkcs12SafeContents certContainer = new();
                    Pkcs12SafeContents keyContainer = new();
                    Pkcs12SafeBag certBag = certContainer.AddCertificate(cert);
                    Pkcs12SafeBag keyBag = keyContainer.AddShroudedKey(key, "", pbe);
                    certBag.Attributes.Add(keyId);
                    keyBag.Attributes.Add(keyId);
                    builder.AddSafeContentsEncrypted(certContainer, "", pbe);
                    builder.AddSafeContentsUnencrypted(keyContainer);

                    builder.SealWithMac("", pbe.HashAlgorithm, pbe.IterationCount);
                    return (builder.Encode(), key);
                }
            }
        }

        internal static (int certs, int keys) VerifyPkcs12(
            byte[] pkcs12,
            string password,
            int expectedIterations,
            HashAlgorithmName expectedMacHashAlgorithm,
            PbeEncryptionAlgorithm expectedEncryptionAlgorithm)
        {
            const string Pkcs7Data = "1.2.840.113549.1.7.1";
            const string Pkcs7Encrypted = "1.2.840.113549.1.7.6";

            Pkcs12Info info = Pkcs12Info.Decode(pkcs12, out int read);
            Assert.Equal(pkcs12.Length, read);
            Assert.Equal(Pkcs12IntegrityMode.Password, info.IntegrityMode);
            Assert.True(info.VerifyMac(password), nameof(info.VerifyMac));

            PfxAsn pfxAsn = PfxAsn.Decode(pkcs12, AsnEncodingRules.BER);
            MacData macData = Assert.NotNull(pfxAsn.MacData);
            AssertExtensions.GreaterThanOrEqualTo(macData.MacSalt.Length, 20);

            Assert.Equal(expectedIterations, macData.IterationCount);
            Assert.Equal(expectedMacHashAlgorithm, HashAlgorithmName.FromOid(macData.Mac.DigestAlgorithm.Algorithm));
            Assert.Null(macData.Mac.DigestAlgorithm.Parameters);

            Assert.Equal(Pkcs7Data, pfxAsn.AuthSafe.ContentType);
            byte[] safeContents = AsnDecoder.ReadOctetString(pfxAsn.AuthSafe.Content.Span, AsnEncodingRules.BER, out _);

            AsnValueReader authSafeReader = new AsnValueReader(safeContents, AsnEncodingRules.BER);
            AsnValueReader sequenceReader = authSafeReader.ReadSequence();
            authSafeReader.ThrowIfNotEmpty();

            int certs = 0;
            int keys = 0;

            while (sequenceReader.HasData)
            {
                ContentInfoAsn.Decode(ref sequenceReader, safeContents, out ContentInfoAsn contentInfo);

                if (contentInfo.ContentType == Pkcs7Encrypted)
                {
                    EncryptedDataAsn encryptedData = EncryptedDataAsn.Decode(contentInfo.Content, AsnEncodingRules.BER);
                    AlgorithmIdentifierAsn algorithmIdentifier = encryptedData.EncryptedContentInfo.ContentEncryptionAlgorithm;
                    AssertEncryptionAlgorithm(algorithmIdentifier);
                }
            }

            foreach (Pkcs12SafeContents pkcs12SafeContents in info.AuthenticatedSafe)
            {
                bool wasEncryptedSafe = false;

                if (pkcs12SafeContents.ConfidentialityMode == Pkcs12ConfidentialityMode.Password)
                {
                    wasEncryptedSafe = true;
                    pkcs12SafeContents.Decrypt(password);
                }

                foreach (Pkcs12SafeBag safeBag in pkcs12SafeContents.GetBags())
                {
                    if (safeBag is Pkcs12ShroudedKeyBag shroudedKeyBag)
                    {
                        EncryptedPrivateKeyInfoAsn epki = EncryptedPrivateKeyInfoAsn.Decode(
                            shroudedKeyBag.EncryptedPkcs8PrivateKey,
                            AsnEncodingRules.BER);
                        AssertEncryptionAlgorithm(epki.EncryptionAlgorithm);
                        keys++;
                    }
                    else if (safeBag is Pkcs12CertBag)
                    {
                        if (wasEncryptedSafe)
                        {
                            certs++;
                        }
                        else if (PlatformDetection.IsWindows10OrLater && !PlatformDetection.IsWindows10Version1703OrGreater)
                        {
                            // Windows 10 before RS2 / 1703 did not encrypt certs, but count them anyway.
                            certs++;
                        }
                    }
                }
            }

            return (certs, keys);

            void AssertEncryptionAlgorithm(AlgorithmIdentifierAsn algorithmIdentifier)
            {
                if (expectedEncryptionAlgorithm == PbeEncryptionAlgorithm.TripleDes3KeyPkcs12)
                {
                    // pbeWithSHA1And3-KeyTripleDES-CBC
                    Assert.Equal("1.2.840.113549.1.12.1.3", algorithmIdentifier.Algorithm);
                    PBEParameter pbeParameter = PBEParameter.Decode(algorithmIdentifier.Parameters.Value, AsnEncodingRules.BER);

                    Assert.Equal(expectedIterations, pbeParameter.IterationCount);
                }
                else
                {
                    Assert.Equal("1.2.840.113549.1.5.13", algorithmIdentifier.Algorithm); // PBES2
                    PBES2Params pbes2Params = PBES2Params.Decode(algorithmIdentifier.Parameters.Value, AsnEncodingRules.BER);
                    Assert.Equal("1.2.840.113549.1.5.12", pbes2Params.KeyDerivationFunc.Algorithm); // PBKDF2
                    Pbkdf2Params pbkdf2Params = Pbkdf2Params.Decode(
                        pbes2Params.KeyDerivationFunc.Parameters.Value,
                        AsnEncodingRules.BER);
                    string expectedEncryptionOid = expectedEncryptionAlgorithm switch
                    {
                        PbeEncryptionAlgorithm.Aes128Cbc => "2.16.840.1.101.3.4.1.2",
                        PbeEncryptionAlgorithm.Aes192Cbc => "2.16.840.1.101.3.4.1.22",
                        PbeEncryptionAlgorithm.Aes256Cbc => "2.16.840.1.101.3.4.1.42",
                        _ => throw new CryptographicException(),
                    };

                    Assert.Equal(expectedIterations, pbkdf2Params.IterationCount);
                    Assert.Equal(expectedMacHashAlgorithm, GetHashAlgorithmFromPbkdf2Params(pbkdf2Params));
                    Assert.Equal(expectedEncryptionOid, pbes2Params.EncryptionScheme.Algorithm);
                }
            }
        }

        private static HashAlgorithmName GetHashAlgorithmFromPbkdf2Params(Pbkdf2Params pbkdf2Params)
        {
            return pbkdf2Params.Prf.Algorithm switch
            {
                "1.2.840.113549.2.7" => HashAlgorithmName.SHA1,
                "1.2.840.113549.2.9" => HashAlgorithmName.SHA256,
                "1.2.840.113549.2.10" => HashAlgorithmName.SHA384,
                "1.2.840.113549.2.11" => HashAlgorithmName.SHA512,
                _ => throw new CryptographicException(),
            };
        }
    }
}
