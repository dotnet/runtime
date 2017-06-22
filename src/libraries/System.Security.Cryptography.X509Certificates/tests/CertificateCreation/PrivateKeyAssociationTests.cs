// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    public static class PrivateKeyAssociationTests
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

        [Theory]
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

                Assert.True(ecdsaOther.VerifyData(data, signature, hashAlgorithm));
            }
        }

        private sealed class RSASha1Pkcs1SignatureGenerator : X509SignatureGenerator
        {
            private readonly X509SignatureGenerator _realRsaGenerator;

            internal RSASha1Pkcs1SignatureGenerator(RSA rsa)
            {
                _realRsaGenerator = X509SignatureGenerator.CreateForRSA(rsa, RSASignaturePadding.Pkcs1);
            }

            protected override PublicKey BuildPublicKey() => _realRsaGenerator.PublicKey;

            public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
            {
                if (hashAlgorithm == HashAlgorithmName.SHA1)
                    return "300D06092A864886F70D0101050500".HexToByteArray();

                throw new InvalidOperationException();
            }

            public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm) =>
                _realRsaGenerator.SignData(data, hashAlgorithm);
        }
    }
}
