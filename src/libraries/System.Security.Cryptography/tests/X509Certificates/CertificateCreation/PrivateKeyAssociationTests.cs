// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography.Tests;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support asymmetric cryptography")]
    public static partial class PrivateKeyAssociationTests
    {
        private const int PROV_RSA_FULL = 1;
        private const int PROV_DSS = 3;
        private const int PROV_DSS_DH = 13;
        private const int PROV_RSA_SCHANNEL = 12;
        private const int PROV_RSA_AES = 24;

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(PROV_RSA_FULL, KeyNumber.Signature)]
        [InlineData(PROV_RSA_FULL, KeyNumber.Exchange)]
        // No PROV_RSA_SIG, creation does not succeed with that prov type, MSDN says it is not supported.
        [InlineData(PROV_RSA_SCHANNEL, KeyNumber.Exchange)]
        [InlineData(PROV_RSA_AES, KeyNumber.Signature)]
        [InlineData(PROV_RSA_AES, KeyNumber.Exchange)]
        public static void AssociatePersistedKey_CAPI_RSA(int provType, KeyNumber keyNumber)
        {
            const string KeyName = nameof(AssociatePersistedKey_CAPI_RSA);

            CspParameters cspParameters = new CspParameters(provType)
            {
                KeyNumber = (int)keyNumber,
                KeyContainerName = KeyName,
                Flags = CspProviderFlags.UseNonExportableKey,
            };

            using (RSACryptoServiceProvider rsaCsp = new RSACryptoServiceProvider(cspParameters))
            {
                rsaCsp.PersistKeyInCsp = false;

                // Use SHA-1 because the FULL and SCHANNEL providers can't handle SHA-2.
                HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA1;
                var generator = new RSASha1Pkcs1SignatureGenerator(rsaCsp);
                byte[] signature;

                CertificateRequest request = new CertificateRequest(
                    new X500DistinguishedName($"CN={KeyName}-{provType}-{keyNumber}"),
                    generator.PublicKey,
                    hashAlgorithm);

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = request.Create(request.SubjectName, generator, now, now.AddDays(1), new byte[1]))
                using (X509Certificate2 withPrivateKey = cert.CopyWithPrivateKey(rsaCsp))
                using (RSA rsa = withPrivateKey.GetRSAPrivateKey())
                {
                    signature = rsa.SignData(Array.Empty<byte>(), hashAlgorithm, RSASignaturePadding.Pkcs1);

                    Assert.True(
                        rsaCsp.VerifyData(Array.Empty<byte>(), signature, hashAlgorithm, RSASignaturePadding.Pkcs1));
                }

                // Some certs have disposed, did they delete the key?
                cspParameters.Flags = CspProviderFlags.UseExistingKey;

                using (RSACryptoServiceProvider stillPersistedKey = new RSACryptoServiceProvider(cspParameters))
                {
                    byte[] signature2 = stillPersistedKey.SignData(
                        Array.Empty<byte>(),
                        hashAlgorithm,
                        RSASignaturePadding.Pkcs1);

                    Assert.Equal(signature, signature2);
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotArm64Process))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/29055")]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(PROV_RSA_FULL, KeyNumber.Signature)]
        [InlineData(PROV_RSA_FULL, KeyNumber.Exchange)]
        // No PROV_RSA_SIG, creation does not succeed with that prov type, MSDN says it is not supported.
        [InlineData(PROV_RSA_SCHANNEL, KeyNumber.Exchange)]
        [InlineData(PROV_RSA_AES, KeyNumber.Signature)]
        [InlineData(PROV_RSA_AES, KeyNumber.Exchange)]
        public static void AssociatePersistedKey_CAPIviaCNG_RSA(int provType, KeyNumber keyNumber)
        {
            const string KeyName = nameof(AssociatePersistedKey_CAPIviaCNG_RSA);

            CspParameters cspParameters = new CspParameters(provType)
            {
                KeyNumber = (int)keyNumber,
                KeyContainerName = KeyName,
                Flags = CspProviderFlags.UseNonExportableKey,
            };

            using (RSACryptoServiceProvider rsaCsp = new RSACryptoServiceProvider(cspParameters))
            {
                rsaCsp.PersistKeyInCsp = false;

                // Use SHA-1 because the FULL and SCHANNEL providers can't handle SHA-2.
                HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA1;
                var generator = new RSASha1Pkcs1SignatureGenerator(rsaCsp);
                byte[] signature;

                CertificateRequest request = new CertificateRequest(
                    $"CN={KeyName}-{provType}-{keyNumber}",
                    rsaCsp,
                    hashAlgorithm,
                    RSASignaturePadding.Pkcs1);

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = request.Create(request.SubjectName, generator, now, now.AddDays(1), new byte[1]))
                using (X509Certificate2 withPrivateKey = cert.CopyWithPrivateKey(rsaCsp))
                using (RSA rsa = withPrivateKey.GetRSAPrivateKey())
                {
                    // `rsa` will be an RSACng wrapping the CAPI key, which means it does not expose the
                    // KeyNumber from CAPI.
                    Assert.IsAssignableFrom<RSACng>(rsa);

                    request = new CertificateRequest(
                        $"CN={KeyName}-{provType}-{keyNumber}-again",
                        rsa,
                        hashAlgorithm,
                        RSASignaturePadding.Pkcs1);

                    X509Certificate2 cert2 = request.Create(
                        request.SubjectName,
                        generator,
                        now,
                        now.AddDays(1),
                        new byte[1]);

                    using (cert2)
                    using (X509Certificate2 withPrivateKey2 = cert2.CopyWithPrivateKey(rsaCsp))
                    using (RSA rsa2 = withPrivateKey2.GetRSAPrivateKey())
                    {
                        signature = rsa2.SignData(
                            Array.Empty<byte>(),
                            hashAlgorithm,
                            RSASignaturePadding.Pkcs1);

                        Assert.True(
                            rsaCsp.VerifyData(
                                Array.Empty<byte>(),
                                signature,
                                hashAlgorithm,
                                RSASignaturePadding.Pkcs1));
                    }
                }

                // Some certs have disposed, did they delete the key?
                cspParameters.Flags = CspProviderFlags.UseExistingKey;

                using (RSACryptoServiceProvider stillPersistedKey = new RSACryptoServiceProvider(cspParameters))
                {
                    byte[] signature2 = stillPersistedKey.SignData(
                        Array.Empty<byte>(),
                        hashAlgorithm,
                        RSASignaturePadding.Pkcs1);

                    Assert.Equal(signature, signature2);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void AssociatePersistedKey_CNG_RSA()
        {
            const string KeyName = nameof(AssociatePersistedKey_CNG_RSA);

            CngKey cngKey = null;
            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;
            byte[] signature;

            try
            {
                CngKeyCreationParameters creationParameters = new CngKeyCreationParameters()
                {
                    ExportPolicy = CngExportPolicies.None,
                    Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                    KeyCreationOptions = CngKeyCreationOptions.OverwriteExistingKey,
                };

                cngKey = CngKey.Create(CngAlgorithm.Rsa, KeyName, creationParameters);

                using (RSACng rsaCng = new RSACng(cngKey))
                {
                    CertificateRequest request = new CertificateRequest(
                        $"CN={KeyName}",
                        rsaCng,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);

                    DateTimeOffset now = DateTimeOffset.UtcNow;

                    using (X509Certificate2 cert = request.CreateSelfSigned(now, now.AddDays(1)))
                    using (RSA rsa = cert.GetRSAPrivateKey())
                    {
                        signature = rsa.SignData(Array.Empty<byte>(), hashAlgorithm, RSASignaturePadding.Pkcs1);

                        Assert.True(
                            rsaCng.VerifyData(Array.Empty<byte>(), signature, hashAlgorithm, RSASignaturePadding.Pkcs1));
                    }
                }

                // Some certs have disposed, did they delete the key?
                using (CngKey stillPersistedKey = CngKey.Open(KeyName, CngProvider.MicrosoftSoftwareKeyStorageProvider))
                using (RSACng rsaCng = new RSACng(stillPersistedKey))
                {
                    byte[] signature2 = rsaCng.SignData(Array.Empty<byte>(), hashAlgorithm, RSASignaturePadding.Pkcs1);

                    Assert.Equal(signature, signature2);
                }
            }
            finally
            {
                cngKey?.Delete();
            }
        }

        [Fact]
        public static void ThirdPartyProvider_RSA()
        {
            using (RSA rsaOther = new RSAOther())
            {
                HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;

                CertificateRequest request = new CertificateRequest(
                    $"CN={nameof(ThirdPartyProvider_RSA)}",
                    rsaOther,
                    hashAlgorithm,
                    RSASignaturePadding.Pkcs1);

                byte[] signature;
                byte[] data = request.SubjectName.RawData;

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = request.CreateSelfSigned(now, now.AddDays(1)))
                {
                    using (RSA rsa = cert.GetRSAPrivateKey())
                    {
                        signature = rsa.SignData(data, hashAlgorithm, RSASignaturePadding.Pkcs1);
                    }

                    // RSAOther is exportable, so ensure PFX export succeeds
                    byte[] pfxBytes = cert.Export(X509ContentType.Pkcs12, request.SubjectName.Name);
                    Assert.InRange(pfxBytes.Length, 100, int.MaxValue);
                }

                Assert.True(rsaOther.VerifyData(data, signature, hashAlgorithm, RSASignaturePadding.Pkcs1));
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(PROV_DSS)]
        [InlineData(PROV_DSS_DH)]
        public static void AssociatePersistedKey_CAPI_DSA(int provType)
        {
            const string KeyName = nameof(AssociatePersistedKey_CAPI_DSA);

            CspParameters cspParameters = new CspParameters(provType)
            {
                KeyContainerName = KeyName,
                Flags = CspProviderFlags.UseNonExportableKey,
            };

            using (DSACryptoServiceProvider dsaCsp = new DSACryptoServiceProvider(cspParameters))
            {
                dsaCsp.PersistKeyInCsp = false;

                X509SignatureGenerator dsaGen = new DSAX509SignatureGenerator(dsaCsp);

                // Use SHA-1 because that's all DSACryptoServiceProvider understands.
                HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA1;

                CertificateRequest request = new CertificateRequest(
                    new X500DistinguishedName($"CN={KeyName}-{provType}"),
                    dsaGen.PublicKey,
                    hashAlgorithm);

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = request.Create(request.SubjectName, dsaGen, now, now.AddDays(1), new byte[1]))
                using (X509Certificate2 certWithPrivateKey = cert.CopyWithPrivateKey(dsaCsp))
                using (DSA dsa = certWithPrivateKey.GetDSAPrivateKey())
                {
                    byte[] signature = dsa.SignData(Array.Empty<byte>(), hashAlgorithm);

                    Assert.True(dsaCsp.VerifyData(Array.Empty<byte>(), signature, hashAlgorithm));
                }

                // Some certs have disposed, did they delete the key?
                cspParameters.Flags = CspProviderFlags.UseExistingKey;

                using (var stillPersistedKey = new DSACryptoServiceProvider(cspParameters))
                {
                    stillPersistedKey.SignData(Array.Empty<byte>(), hashAlgorithm);
                }
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(PROV_DSS)]
        [InlineData(PROV_DSS_DH)]
        public static void AssociatePersistedKey_CAPIviaCNG_DSA(int provType)
        {
            const string KeyName = nameof(AssociatePersistedKey_CAPIviaCNG_DSA);

            CspParameters cspParameters = new CspParameters(provType)
            {
                KeyContainerName = KeyName,
                Flags = CspProviderFlags.UseNonExportableKey,
            };

            using (DSACryptoServiceProvider dsaCsp = new DSACryptoServiceProvider(cspParameters))
            {
                dsaCsp.PersistKeyInCsp = false;

                X509SignatureGenerator dsaGen = new DSAX509SignatureGenerator(dsaCsp);

                // Use SHA-1 because that's all DSACryptoServiceProvider understands.
                HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA1;
                byte[] signature;

                CertificateRequest request = new CertificateRequest(
                    new X500DistinguishedName($"CN={KeyName}-{provType}"),
                    dsaGen.PublicKey,
                    hashAlgorithm);

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = request.Create(request.SubjectName, dsaGen, now, now.AddDays(1), new byte[1]))
                using (X509Certificate2 certWithPrivateKey = cert.CopyWithPrivateKey(dsaCsp))
                using (DSA dsa = certWithPrivateKey.GetDSAPrivateKey())
                {
                    // `dsa` will be an DSACng wrapping the CAPI key
                    Assert.IsAssignableFrom<DSACng>(dsa);

                    request = new CertificateRequest(
                        new X500DistinguishedName($"CN={KeyName}-{provType}-again"),
                        dsaGen.PublicKey,
                        hashAlgorithm);

                    using (X509Certificate2 cert2 = request.Create(request.SubjectName, dsaGen, now, now.AddDays(1), new byte[1]))
                    using (X509Certificate2 cert2WithPrivateKey = cert2.CopyWithPrivateKey(dsa))
                    using (DSA dsa2 = cert2WithPrivateKey.GetDSAPrivateKey())
                    {
                        signature = dsa2.SignData(Array.Empty<byte>(), hashAlgorithm);

                        Assert.True(dsaCsp.VerifyData(Array.Empty<byte>(), signature, hashAlgorithm));
                    }
                }

                // Some certs have disposed, did they delete the key?
                cspParameters.Flags = CspProviderFlags.UseExistingKey;

                using (var stillPersistedKey = new DSACryptoServiceProvider(cspParameters))
                {
                    stillPersistedKey.SignData(Array.Empty<byte>(), hashAlgorithm);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void AssociatePersistedKey_CNG_DSA()
        {
            const string KeyName = nameof(AssociatePersistedKey_CNG_DSA);

            CngKey cngKey = null;
            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;
            byte[] signature;

            try
            {
                CngKeyCreationParameters creationParameters = new CngKeyCreationParameters()
                {
                    ExportPolicy = CngExportPolicies.None,
                    Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                    KeyCreationOptions = CngKeyCreationOptions.OverwriteExistingKey,
                    Parameters =
                    {
                        new CngProperty("Length", BitConverter.GetBytes(1024), CngPropertyOptions.None),
                    }
                };

                cngKey = CngKey.Create(new CngAlgorithm("DSA"), KeyName, creationParameters);

                using (DSACng dsaCng = new DSACng(cngKey))
                {
                    X509SignatureGenerator dsaGen = new DSAX509SignatureGenerator(dsaCng);

                    CertificateRequest request = new CertificateRequest(
                        new X500DistinguishedName($"CN={KeyName}"),
                        dsaGen.PublicKey,
                        HashAlgorithmName.SHA256);

                    DateTimeOffset now = DateTimeOffset.UtcNow;

                    using (X509Certificate2 cert = request.Create(request.SubjectName, dsaGen, now, now.AddDays(1), new byte[1]))
                    using (X509Certificate2 certWithPrivateKey = cert.CopyWithPrivateKey(dsaCng))
                    using (DSA dsa = certWithPrivateKey.GetDSAPrivateKey())
                    {
                        signature = dsa.SignData(Array.Empty<byte>(), hashAlgorithm);

                        Assert.True(dsaCng.VerifyData(Array.Empty<byte>(), signature, hashAlgorithm));
                    }
                }

                // Some certs have disposed, did they delete the key?
                using (CngKey stillPersistedKey = CngKey.Open(KeyName, CngProvider.MicrosoftSoftwareKeyStorageProvider))
                using (DSACng dsaCng = new DSACng(stillPersistedKey))
                {
                    dsaCng.SignData(Array.Empty<byte>(), hashAlgorithm);
                }
            }
            finally
            {
                cngKey?.Delete();
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void ThirdPartyProvider_DSA()
        {
            using (DSA dsaOther = new DSAOther())
            {
                dsaOther.ImportParameters(TestData.GetDSA1024Params());

                X509SignatureGenerator dsaGen = new DSAX509SignatureGenerator(dsaOther);

                // macOS DSA is limited to FIPS 186-3.
                HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA1;

                CertificateRequest request = new CertificateRequest(
                    new X500DistinguishedName($"CN={nameof(ThirdPartyProvider_DSA)}"),
                    dsaGen.PublicKey,
                    hashAlgorithm);

                byte[] signature;
                byte[] data = request.SubjectName.RawData;

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = request.Create(request.SubjectName, dsaGen, now, now.AddDays(1), new byte[1]))
                using (X509Certificate2 certWithPrivateKey = cert.CopyWithPrivateKey(dsaOther))
                {
                    using (DSA dsa = certWithPrivateKey.GetDSAPrivateKey())
                    {
                        signature = dsa.SignData(data, hashAlgorithm);
                    }

                    // DSAOther is exportable, so ensure PFX export succeeds
                    byte[] pfxBytes = certWithPrivateKey.Export(X509ContentType.Pkcs12, request.SubjectName.Name);
                    Assert.InRange(pfxBytes.Length, 100, int.MaxValue);
                }

                Assert.True(dsaOther.VerifyData(data, signature, hashAlgorithm));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void AssociatePersistedKey_CNG_ECDsa()
        {
            const string KeyName = nameof(AssociatePersistedKey_CNG_ECDsa);

            CngKey cngKey = null;
            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;
            byte[] signature;

            try
            {
                CngKeyCreationParameters creationParameters = new CngKeyCreationParameters()
                {
                    ExportPolicy = CngExportPolicies.None,
                    Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                    KeyCreationOptions = CngKeyCreationOptions.OverwriteExistingKey,
                };

                cngKey = CngKey.Create(CngAlgorithm.ECDsaP384, KeyName, creationParameters);

                using (ECDsaCng ecdsaCng = new ECDsaCng(cngKey))
                {
                    CertificateRequest request = new CertificateRequest(
                        new X500DistinguishedName($"CN={KeyName}"),
                        ecdsaCng,
                        HashAlgorithmName.SHA256);

                    DateTimeOffset now = DateTimeOffset.UtcNow;

                    using (X509Certificate2 cert = request.CreateSelfSigned(now, now.AddDays(1)))
                    using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
                    {
                        signature = ecdsa.SignData(Array.Empty<byte>(), hashAlgorithm);

                        Assert.True(ecdsaCng.VerifyData(Array.Empty<byte>(), signature, hashAlgorithm));
                    }
                }

                // Some certs have disposed, did they delete the key?
                using (CngKey stillPersistedKey = CngKey.Open(KeyName, CngProvider.MicrosoftSoftwareKeyStorageProvider))
                using (ECDsaCng ecdsaCng = new ECDsaCng(stillPersistedKey))
                {
                    ecdsaCng.SignData(Array.Empty<byte>(), hashAlgorithm);
                }
            }
            finally
            {
                cngKey?.Delete();
            }
        }

        [Fact]
        public static void ThirdPartyProvider_ECDsa()
        {
            using (ECDsaOther ecdsaOther = new ECDsaOther())
            {
                HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;

                CertificateRequest request = new CertificateRequest(
                    new X500DistinguishedName($"CN={nameof(ThirdPartyProvider_ECDsa)}"),
                    ecdsaOther,
                    hashAlgorithm);

                byte[] signature;
                byte[] data = request.SubjectName.RawData;

                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (X509Certificate2 cert = request.CreateSelfSigned(now, now.AddDays(1)))
                {
                    using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
                    {
                        signature = ecdsa.SignData(data, hashAlgorithm);
                    }

                    // ECDsaOther is exportable, so ensure PFX export succeeds
                    byte[] pfxBytes = cert.Export(X509ContentType.Pkcs12, request.SubjectName.Name);
                    Assert.InRange(pfxBytes.Length, 100, int.MaxValue);
                }

                AssertExtensions.TrueExpression(ecdsaOther.VerifyData(data, signature, hashAlgorithm));
            }
        }

        [Fact]
        public static void CheckCopyWithPrivateKey_RSA()
        {
            using (X509Certificate2 withKey = X509CertificateLoader.LoadPkcs12(TestData.PfxData, TestData.PfxDataPassword))
            using (X509Certificate2 pubOnly = X509CertificateLoader.LoadCertificate(withKey.RawDataMemory.Span))
            using (RSA privKey = withKey.GetRSAPrivateKey())
            using (X509Certificate2 wrongAlg = X509Certificate2.CreateFromPem(TestData.EcDhCertificate))
            {
                CheckCopyWithPrivateKey(
                    pubOnly,
                    wrongAlg,
                    privKey,
                    [
                        () => RSA.Create(2048),
                        () => RSA.Create(4096)
                    ],
                    RSACertificateExtensions.CopyWithPrivateKey,
                    RSACertificateExtensions.GetRSAPublicKey,
                    RSACertificateExtensions.GetRSAPrivateKey,
                    (priv, pub) =>
                    {
                        byte[] data = new byte[RandomNumberGenerator.GetInt32(97)];
                        RandomNumberGenerator.Fill(data);

                        byte[] signature = priv.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                        AssertExtensions.TrueExpression(pub.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
                    });
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void CheckCopyWithPrivateKey_DSA()
        {
            using (X509Certificate2 withKey = X509CertificateLoader.LoadPkcs12(TestData.Dsa1024Pfx, TestData.Dsa1024PfxPassword))
            using (X509Certificate2 pubOnly = X509CertificateLoader.LoadCertificate(withKey.RawDataMemory.Span))
            using (DSA privKey = withKey.GetDSAPrivateKey())
            using (X509Certificate2 wrongAlg = X509Certificate2.CreateFromPem(TestData.EcDhCertificate))
            {
                CheckCopyWithPrivateKey(
                    pubOnly,
                    wrongAlg,
                    privKey,
                    [
                        () =>
                        {
                            DSA dsa = DSA.Create();
                            dsa.ImportParameters(TestData.GetDSA1024Params());
                            return dsa;
                        },
                        () =>
                        {
                            DSA dsa = DSA.Create();

                            if (Dsa.Tests.DSASignVerify.SupportsFips186_3)
                            {
                                dsa.ImportParameters(Dsa.Tests.DSATestData.GetDSA2048Params());
                            }
                            else
                            {
                                dsa.ImportParameters(Dsa.Tests.DSATestData.Dsa576Parameters);
                            }

                            return dsa;
                        }
                    ],
                    DSACertificateExtensions.CopyWithPrivateKey,
                    DSACertificateExtensions.GetDSAPublicKey,
                    DSACertificateExtensions.GetDSAPrivateKey,
                    (priv, pub) =>
                    {
                        byte[] data = new byte[RandomNumberGenerator.GetInt32(97)];
                        RandomNumberGenerator.Fill(data);

                        byte[] signature = priv.SignData(data, HashAlgorithmName.SHA1);
                        AssertExtensions.TrueExpression(pub.VerifyData(data, signature, HashAlgorithmName.SHA1));
                    });
            }
        }

        [Fact]
        public static void CheckCopyWithPrivateKey_ECDSA()
        {
            // A plain "ecPublicKey" cert can be either ECDSA or ECDH, but EcDhCertificate has a KeyUsage that
            // says it is not suitable for being ECDSA.
            // that stop them from being interchangeable, making them a much better test case than (e.g.) RSA
            using (X509Certificate2 pubOnly = X509Certificate2.CreateFromPem(TestData.ECDsaCertificate))
            using (ECDsa privKey = ECDsa.Create())
            using (X509Certificate2 wrongAlg = X509Certificate2.CreateFromPem(TestData.EcDhCertificate))
            {
                privKey.ImportFromPem(TestData.ECDsaECPrivateKey);

                CheckCopyWithPrivateKey(
                    pubOnly,
                    wrongAlg,
                    privKey,
                    [
                        () => ECDsa.Create(ECCurve.NamedCurves.nistP256),
                        () => ECDsa.Create(ECCurve.NamedCurves.nistP384),
                        () => ECDsa.Create(ECCurve.NamedCurves.nistP521),
                    ],
                    ECDsaCertificateExtensions.CopyWithPrivateKey,
                    ECDsaCertificateExtensions.GetECDsaPublicKey,
                    ECDsaCertificateExtensions.GetECDsaPrivateKey,
                    (priv, pub) =>
                    {
                        byte[] data = new byte[RandomNumberGenerator.GetInt32(97)];
                        RandomNumberGenerator.Fill(data);

                        byte[] signature = priv.SignData(data, HashAlgorithmName.SHA256);
                        AssertExtensions.TrueExpression(pub.VerifyData(data, signature, HashAlgorithmName.SHA256));
                    });
            }
        }

        [Fact]
        public static void CheckCopyWithPrivateKey_ECDH()
        {
            // The ECDH methods don't reject certs that lack the KeyAgreement KU, so test EC-DH vs RSA.
            using (X509Certificate2 pubOnly = X509Certificate2.CreateFromPem(TestData.EcDhCertificate))
            using (ECDiffieHellman privKey = ECDiffieHellman.Create())
            using (X509Certificate2 wrongAlg = X509CertificateLoader.LoadCertificate(TestData.CertWithEnhancedKeyUsage))
            {
                privKey.ImportFromPem(TestData.EcDhPkcs8Key);

                CheckCopyWithPrivateKey(
                    pubOnly,
                    wrongAlg,
                    privKey,
                    [
                        () => ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256),
                        () => ECDiffieHellman.Create(ECCurve.NamedCurves.nistP384),
                        () => ECDiffieHellman.Create(ECCurve.NamedCurves.nistP521),
                    ],
                    (cert, ecdh) => cert.CopyWithPrivateKey(ecdh),
                    cert => cert.GetECDiffieHellmanPublicKey(),
                    cert => cert.GetECDiffieHellmanPrivateKey(),
                    (priv, pub) =>
                    {
                        ECParameters ecParams = pub.ExportParameters(false);

                        using (ECDiffieHellman other = ECDiffieHellman.Create(ecParams.Curve))
                        using (ECDiffieHellmanPublicKey otherPub = other.PublicKey)
                        using (ECDiffieHellmanPublicKey usPub = pub.PublicKey)
                        {
                            byte[] otherToUs = other.DeriveKeyFromHash(usPub, HashAlgorithmName.SHA256);
                            byte[] usToOther = priv.DeriveKeyFromHash(otherPub, HashAlgorithmName.SHA256);

                            AssertExtensions.SequenceEqual(otherToUs, usToOther);
                        }
                    });
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void GetMLDsaPublicKeyTest()
        {
            // Cert without private key
            using (X509Certificate2 cert = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
            using (MLDsa? certKey = GetMLDsaPublicKey(cert))
            {
                Assert.NotNull(certKey);
                byte[] publicKey = new byte[certKey.Algorithm.PublicKeySizeInBytes];
                Assert.Equal(publicKey.Length, certKey.ExportMLDsaPublicKey(publicKey));
                AssertExtensions.SequenceEqual(MLDsaTestsData.IetfMLDsa44.PublicKey, publicKey);

                // Verify the key is not actually private
                byte[] signature = new byte[certKey.Algorithm.SignatureSizeInBytes];
                Assert.ThrowsAny<CryptographicException>(() => certKey.SignData([1, 2, 3], signature));
            }

            // Cert with private key
            using (X509Certificate2 cert = LoadMLDsaIetfCertificateWithPrivateKey())
            using (MLDsa? certKey = GetMLDsaPublicKey(cert))
            {
                Assert.NotNull(certKey);
                byte[] publicKey = new byte[certKey.Algorithm.PublicKeySizeInBytes];
                Assert.Equal(publicKey.Length, certKey.ExportMLDsaPublicKey(publicKey));
                AssertExtensions.SequenceEqual(MLDsaTestsData.IetfMLDsa44.PublicKey, publicKey);

                // Verify the key is not actually private
                byte[] signature = new byte[certKey.Algorithm.SignatureSizeInBytes];
                Assert.ThrowsAny<CryptographicException>(() => certKey.SignData([1, 2, 3], signature));
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void GetMLDsaPrivateKeyTest()
        {
            // Cert without private key
            using (X509Certificate2 cert = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
            {
                using (MLDsa? certKey = GetMLDsaPrivateKey(cert))
                {
                    Assert.Null(certKey);
                }
            }

            // Cert with private key
            using (X509Certificate2 certWithPrivateKey = LoadMLDsaIetfCertificateWithPrivateKey())
            {
                using (MLDsa? certKey = GetMLDsaPrivateKey(certWithPrivateKey))
                {
                    Assert.NotNull(certKey);

                    // Verify the key is actually private
                    byte[] privateSeed = new byte[certKey.Algorithm.PrivateSeedSizeInBytes];
                    Assert.Equal(privateSeed.Length, certKey.ExportMLDsaPrivateSeed(privateSeed));
                    AssertExtensions.SequenceEqual(
                        MLDsaTestsData.IetfMLDsa44.PrivateSeed,
                        privateSeed);
                }
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void CheckCopyWithPrivateKey_MLDSA()
        {
            using (MLDsa privKey = MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa65))
            {
                CertificateRequest req = new CertificateRequest($"CN={nameof(CheckCopyWithPrivateKey_MLDSA)}", privKey);
                DateTimeOffset now = DateTimeOffset.UtcNow;

                X509Certificate2 pubOnly = req.Create(
                    req.SubjectName,
                    X509SignatureGenerator.CreateForMLDsa(privKey),
                    now.AddMinutes(-10),
                    now.AddMinutes(10),
                    new byte[] { 2, 4, 6, 8, 9, 7, 5, 3, 1 });

                using (pubOnly)
                using (X509Certificate2 wrongAlg = X509CertificateLoader.LoadCertificate(TestData.CertWithEnhancedKeyUsage))
                {
                    CheckCopyWithPrivateKey(
                    pubOnly,
                    wrongAlg,
                    privKey,
                    [
                        () => MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa44),
                        () => MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa65),
                        () => MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa87),
                    ],
                    (cert, key) => cert.CopyWithPrivateKey(key),
                    cert => cert.GetMLDsaPublicKey(),
                    cert => cert.GetMLDsaPrivateKey(),
                    (priv, pub) =>
                    {
                        byte[] data = new byte[RandomNumberGenerator.GetInt32(97)];
                        RandomNumberGenerator.Fill(data);

                        byte[] signature = new byte[pub.Algorithm.SignatureSizeInBytes];
                        int written = priv.SignData(data, signature);
                        Assert.Equal(signature.Length, written);
                        Assert.True(pub.VerifyData(data, signature));
                    });
                }
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void CheckCopyWithPrivateKey_MLDsa_OtherMLDsa_SecretKey()
        {
            using (X509Certificate2 pubOnly = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
            {
                using (MLDsaTestImplementation publicMLDsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa44))
                {
                    Exception e = new Exception("no secret key");

                    // The private key can be retrieved directly or from PKCS#8. If the seed is not available,
                    // it should fall back to the secret key.
                    publicMLDsa.TryExportPkcs8PrivateKeyHook = (_, out _) => throw e;
                    publicMLDsa.ExportMLDsaSecretKeyHook = _ => throw e;
                    publicMLDsa.ExportMLDsaPrivateSeedHook = _ => throw new CryptographicException("Should signal to try secret key");
                    publicMLDsa.ExportMLDsaPublicKeyHook = (Span<byte> destination) =>
                        MLDsaTestsData.IetfMLDsa44.PublicKey.CopyTo(destination);

                    Assert.Same(e, AssertExtensions.Throws<Exception>(() => CopyWithPrivateKey_MLDsa(pubOnly, publicMLDsa)));
                }

                MLDsaTestImplementation privateMLDsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa44);
                privateMLDsa.ExportMLDsaPrivateSeedHook = _ => throw new CryptographicException("Should signal to try secret key"); ;
                privateMLDsa.ExportMLDsaPublicKeyHook = (Span<byte> destination) =>
                    MLDsaTestsData.IetfMLDsa44.PublicKey.CopyTo(destination);
                privateMLDsa.ExportMLDsaSecretKeyHook = (Span<byte> destination) =>
                    MLDsaTestsData.IetfMLDsa44.SecretKey.CopyTo(destination);

                privateMLDsa.TryExportPkcs8PrivateKeyHook = (dest, out written) =>
                {
                    if (MLDsaTestsData.IetfMLDsa44.Pkcs8PrivateKey_Seed.AsSpan().TryCopyTo(dest))
                    {
                        written = MLDsaTestsData.IetfMLDsa44.Pkcs8PrivateKey_Seed.Length;
                        return true;
                    }

                    written = 0;
                    return false;
                };

                using (X509Certificate2 privCert = CopyWithPrivateKey_MLDsa(pubOnly, privateMLDsa))
                {
                    AssertExtensions.TrueExpression(privCert.HasPrivateKey);

                    using (MLDsa certPrivateMLDsa = GetMLDsaPrivateKey(privCert))
                    {
                        byte[] secretKey = new byte[certPrivateMLDsa.Algorithm.SecretKeySizeInBytes];
                        Assert.Equal(secretKey.Length, certPrivateMLDsa.ExportMLDsaSecretKey(secretKey));
                        AssertExtensions.SequenceEqual(
                            MLDsaTestsData.IetfMLDsa44.SecretKey,
                            secretKey);

                        privateMLDsa.Dispose();
                        privateMLDsa.ExportMLDsaPrivateSeedHook = _ => Assert.Fail();
                        privateMLDsa.ExportMLDsaPublicKeyHook = _ => Assert.Fail();
                        privateMLDsa.ExportMLDsaSecretKeyHook = _ => Assert.Fail();
                        privateMLDsa.TryExportPkcs8PrivateKeyHook = (_, out w) => { Assert.Fail(); w = 0; return false; };

                        // Ensure the key is actual a clone
                        Assert.Equal(secretKey.Length, certPrivateMLDsa.ExportMLDsaSecretKey(secretKey));
                        AssertExtensions.SequenceEqual(
                            MLDsaTestsData.IetfMLDsa44.SecretKey,
                            secretKey);
                    }
                }
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void CheckCopyWithPrivateKey_MLDsa_OtherMLDsa_PrivateSeed()
        {
            using (X509Certificate2 pubOnly = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
            {
                using (MLDsaTestImplementation publicMLDsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa44))
                {
                    Exception e = new Exception("no secret key");

                    // The private seed can be retrieved directly or from PKCS#8
                    publicMLDsa.TryExportPkcs8PrivateKeyHook = (_, out _) => throw e;
                    publicMLDsa.ExportMLDsaPrivateSeedHook = _ => throw e;
                    publicMLDsa.ExportMLDsaPublicKeyHook = (Span<byte> destination) =>
                        MLDsaTestsData.IetfMLDsa44.PublicKey.CopyTo(destination);

                    Assert.Same(e, AssertExtensions.Throws<Exception>(() => CopyWithPrivateKey_MLDsa(pubOnly, publicMLDsa)));
                }

                MLDsaTestImplementation privateMLDsa = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa44);
                privateMLDsa.ExportMLDsaPublicKeyHook = (Span<byte> destination) =>
                    MLDsaTestsData.IetfMLDsa44.PublicKey.CopyTo(destination);
                privateMLDsa.ExportMLDsaPrivateSeedHook = (Span<byte> destination) =>
                    MLDsaTestsData.IetfMLDsa44.PrivateSeed.CopyTo(destination);

                privateMLDsa.TryExportPkcs8PrivateKeyHook = (dest, out written) =>
                {
                    if (MLDsaTestsData.IetfMLDsa44.Pkcs8PrivateKey_Seed.AsSpan().TryCopyTo(dest))
                    {
                        written = MLDsaTestsData.IetfMLDsa44.Pkcs8PrivateKey_Seed.Length;
                        return true;
                    }

                    written = 0;
                    return false;
                };

                using (X509Certificate2 privCert = CopyWithPrivateKey_MLDsa(pubOnly, privateMLDsa))
                {
                    AssertExtensions.TrueExpression(privCert.HasPrivateKey);

                    using (MLDsa certPrivateMLDsa = GetMLDsaPrivateKey(privCert))
                    {
                        byte[] secretKey = new byte[certPrivateMLDsa.Algorithm.PrivateSeedSizeInBytes];
                        Assert.Equal(secretKey.Length, certPrivateMLDsa.ExportMLDsaPrivateSeed(secretKey));
                        AssertExtensions.SequenceEqual(
                            MLDsaTestsData.IetfMLDsa44.PrivateSeed,
                            secretKey);

                        privateMLDsa.Dispose();
                        privateMLDsa.ExportMLDsaPublicKeyHook = _ => Assert.Fail();
                        privateMLDsa.ExportMLDsaPrivateSeedHook = _ => Assert.Fail();
                        privateMLDsa.TryExportPkcs8PrivateKeyHook = (_, out w) => { Assert.Fail(); w = 0; return false; };

                        // Ensure the key is actual a clone
                        Assert.Equal(secretKey.Length, certPrivateMLDsa.ExportMLDsaPrivateSeed(secretKey));
                        AssertExtensions.SequenceEqual(
                            MLDsaTestsData.IetfMLDsa44.PrivateSeed,
                            secretKey);
                    }
                }
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void CheckCopyWithPrivateKey_MLDsa_OtherMLDsa_WrongPkcs8()
        {
            using (X509Certificate2 ietfCert = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
            using (RSA rsa = RSA.Create())
            using (MLDsaTestImplementation keyThatExportsRsaPkcs8 = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa44))
            {
                keyThatExportsRsaPkcs8.ExportMLDsaPublicKeyHook = MLDsaTestsData.IetfMLDsa44.PublicKey.CopyTo;
                keyThatExportsRsaPkcs8.ExportMLDsaPrivateSeedHook = MLDsaTestsData.IetfMLDsa44.PrivateSeed.CopyTo;

                // Export RSA PKCS#8
                keyThatExportsRsaPkcs8.TryExportPkcs8PrivateKeyHook = rsa.TryExportPkcs8PrivateKey;

                if (PlatformDetection.IsWindows)
                {
                    // Only Windows uses PKCS#8 for pairing key to cert.
                    AssertExtensions.Throws<CryptographicException>(() => ietfCert.CopyWithPrivateKey(keyThatExportsRsaPkcs8));
                }
                else
                {
                    // Assert.NoThrow
                    using (ietfCert.CopyWithPrivateKey(keyThatExportsRsaPkcs8)) { }
                }
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void CheckCopyWithPrivateKey_MLDsa_OtherMLDsa_MalformedPkcs8()
        {
            using (X509Certificate2 ietfCert = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
            using (MLDsaTestImplementation keyThatExportsMalformedPkcs8 = MLDsaTestImplementation.CreateOverriddenCoreMethodsFail(MLDsaAlgorithm.MLDsa44))
            {
                keyThatExportsMalformedPkcs8.ExportMLDsaPublicKeyHook = MLDsaTestsData.IetfMLDsa44.PublicKey.CopyTo;
                keyThatExportsMalformedPkcs8.ExportMLDsaPrivateSeedHook = MLDsaTestsData.IetfMLDsa44.PrivateSeed.CopyTo;

                // Export malformed PKCS#8
                keyThatExportsMalformedPkcs8.TryExportPkcs8PrivateKeyHook =
                    (dest, out written) =>
                    {
                        written = 0;
                        return true;
                    };

                if (PlatformDetection.IsWindows)
                {
                    // Only Windows uses PKCS#8 for pairing key to cert.
                    AssertExtensions.Throws<CryptographicException>(() => ietfCert.CopyWithPrivateKey(keyThatExportsMalformedPkcs8));
                }
                else
                {
                    // Assert.NoThrow
                    using (ietfCert.CopyWithPrivateKey(keyThatExportsMalformedPkcs8)) { }
                }
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void AssociatePersistedKey_CNG_MLDsa()
        {
            const string KeyName = nameof(AssociatePersistedKey_CNG_MLDsa);

            CngKey cngKey = null;
            byte[] signature = new byte[MLDsaAlgorithm.MLDsa44.SignatureSizeInBytes];

            try
            {
                CngKeyCreationParameters creationParameters = new CngKeyCreationParameters()
                {
                    ExportPolicy = CngExportPolicies.None,
                    Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                    KeyCreationOptions = CngKeyCreationOptions.OverwriteExistingKey,
                };

                CngProperty parameterSet = MLDsaTestHelpers.GetCngProperty(MLDsaAlgorithm.MLDsa44);
                creationParameters.Parameters.Add(parameterSet);

                cngKey = CngKey.Create(CngAlgorithm.MLDsa, KeyName, creationParameters);

                using (MLDsaCng mldsaCng = new MLDsaCng(cngKey))
                {
                    CertificateRequest request = new CertificateRequest(
                        new X500DistinguishedName($"CN={KeyName}"),
                        mldsaCng);

                    DateTimeOffset now = DateTimeOffset.UtcNow;

                    using (X509Certificate2 cert = request.CreateSelfSigned(now, now.AddDays(1)))
                    using (MLDsa mldsa = cert.GetMLDsaPrivateKey())
                    {
                        mldsa.SignData("test"u8, signature);
                        Assert.True(mldsaCng.VerifyData("test"u8, signature));
                    }
                }

                // Some certs have disposed, did they delete the key?
                using (CngKey stillPersistedKey = CngKey.Open(KeyName, CngProvider.MicrosoftSoftwareKeyStorageProvider))
                using (MLDsaCng mldsa = new MLDsaCng(stillPersistedKey))
                {
                    mldsa.SignData("test"u8, signature);
                }
            }
            finally
            {
                cngKey?.Delete();
            }
        }

        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void AssociateEphemeralKey_CNG_MLDsa()
        {
            CngKeyCreationParameters creationParameters = new CngKeyCreationParameters();
            CngProperty parameterSet = MLDsaTestHelpers.GetCngProperty(MLDsaAlgorithm.MLDsa44);
            creationParameters.Parameters.Add(parameterSet);
            creationParameters.ExportPolicy = CngExportPolicies.AllowPlaintextExport;

            byte[] signature = new byte[MLDsaAlgorithm.MLDsa44.SignatureSizeInBytes];

            using (CngKey ephemeralCngKey = CngKey.Create(CngAlgorithm.MLDsa, keyName: null, creationParameters))
            {
                using (MLDsaCng mldsaCng = new MLDsaCng(ephemeralCngKey))
                {
                    CertificateRequest request = new CertificateRequest(
                        new X500DistinguishedName($"CN=EphemeralMLDsaKey"),
                        mldsaCng);

                    DateTimeOffset now = DateTimeOffset.UtcNow;

                    using (X509Certificate2 cert = request.CreateSelfSigned(now, now.AddDays(1)))
                    using (MLDsa mldsa = cert.GetMLDsaPrivateKey())
                    {
                        mldsa.SignData("test"u8, signature);
                        Assert.True(mldsaCng.VerifyData("test"u8, signature));
                    }

                    // Run a few iterations to catch nondeterministic use-after-dispose issues
                    for (int i = 0; i < 5; i++)
                    {
                        using (X509Certificate2 cert = request.CreateSelfSigned(now, now.AddDays(1))) { }
                    }

                    mldsaCng.SignData("test"u8, signature);
                }

                // Some certs have disposed, did they delete the key?
                using (MLDsaCng mldsa = new MLDsaCng(ephemeralCngKey))
                {
                    mldsa.SignData("test"u8, signature);
                }
            }
        }

        private static partial Func<X509Certificate2, MLKem, X509Certificate2> CopyWithPrivateKey_MLKem =>
            (cert, key) => cert.CopyWithPrivateKey(key);

        private static partial Func<X509Certificate2, MLKem> GetMLKemPublicKey =>
            cert => cert.GetMLKemPublicKey();

        private static partial Func<X509Certificate2, MLKem> GetMLKemPrivateKey =>
            cert => cert.GetMLKemPrivateKey();

        private static Func<X509Certificate2, MLDsa, X509Certificate2> CopyWithPrivateKey_MLDsa =>
            (cert, key) => cert.CopyWithPrivateKey(key);

        private static Func<X509Certificate2, MLDsa> GetMLDsaPublicKey =>
            cert => cert.GetMLDsaPublicKey();

        private static Func<X509Certificate2, MLDsa> GetMLDsaPrivateKey =>
            cert => cert.GetMLDsaPrivateKey();

        private static partial Func<X509Certificate2, SlhDsa, X509Certificate2> CopyWithPrivateKey_SlhDsa =>
            (cert, key) => cert.CopyWithPrivateKey(key);

        private static partial Func<X509Certificate2, SlhDsa> GetSlhDsaPublicKey =>
            cert => cert.GetSlhDsaPublicKey();

        private static partial Func<X509Certificate2, SlhDsa> GetSlhDsaPrivateKey =>
            cert => cert.GetSlhDsaPrivateKey();

        private static partial void CheckCopyWithPrivateKey<TKey>(
            X509Certificate2 cert,
            X509Certificate2 wrongAlgorithmCert,
            TKey correctPrivateKey,
            IEnumerable<Func<TKey>> incorrectKeys,
            Func<X509Certificate2, TKey, X509Certificate2> copyWithPrivateKey,
            Func<X509Certificate2, TKey> getPublicKey,
            Func<X509Certificate2, TKey> getPrivateKey,
            Action<TKey, TKey> keyProver)
            where TKey : class, IDisposable;

        private static X509Certificate2 LoadMLDsaIetfCertificateWithPrivateKey()
        {
            using (X509Certificate2 cert = X509CertificateLoader.LoadCertificate(MLDsaTestsData.IetfMLDsa44.Certificate))
            using (MLDsa? privateKey = MLDsa.ImportMLDsaPrivateSeed(MLDsaAlgorithm.MLDsa44, MLDsaTestsData.IetfMLDsa44.PrivateSeed))
                return cert.CopyWithPrivateKey(privateKey);
        }
    }
}
