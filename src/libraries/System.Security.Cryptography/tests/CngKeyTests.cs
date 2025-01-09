// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public class CngKeyTests
    {
        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsVbsAvailable))]
        [InlineData(CngKeyCreationOptions.PreferVbs)]
        [InlineData(CngKeyCreationOptions.RequireVbs)]
        [InlineData(CngKeyCreationOptions.UsePerBootKey)]
        public void CreateVbsKey_SignAndVerify(CngKeyCreationOptions creationOption)
        {
            using (CngKeyWrapper key = CngKeyWrapper.CreateMicrosoftSoftwareKeyStorageProvider(
                CngAlgorithm.ECDsaP256,
                creationOption,
                keySuffix: creationOption.ToString()))
            {
                SignAndVerifyECDsa(key.Key);
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsVbsAvailable))]
        [InlineData(CngKeyCreationOptions.PreferVbs)]
        [InlineData(CngKeyCreationOptions.RequireVbs)]
        [InlineData(CngKeyCreationOptions.UsePerBootKey)]
        public void CreateVbsKey_KeyIsNotExportable(CngKeyCreationOptions creationOption)
        {
            using (CngKeyWrapper key = CngKeyWrapper.CreateMicrosoftSoftwareKeyStorageProvider(
                CngAlgorithm.ECDsaP256,
                creationOption,
                keySuffix: creationOption.ToString()))
            {
                using (ECDsaCng ecdsa = new ECDsaCng(key.Key))
                {
                    Assert.ThrowsAny<CryptographicException>(() => ecdsa.ExportExplicitParameters(includePrivateParameters: true));
                }
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.IsVbsAvailable))]
        [InlineData(CngKeyCreationOptions.PreferVbs)]
        [InlineData(CngKeyCreationOptions.RequireVbs)]
        [InlineData(CngKeyCreationOptions.UsePerBootKey)]
        [InlineData(CngKeyCreationOptions.PreferVbs | CngKeyCreationOptions.UsePerBootKey)]
        [InlineData(CngKeyCreationOptions.RequireVbs | CngKeyCreationOptions.UsePerBootKey)]
        public void CreateVbsKey_SoftwareKeyStorageProviderFlagsOnWrongProvider(CngKeyCreationOptions creationOption)
        {
            Assert.ThrowsAny<CryptographicException>(() => CngKeyWrapper.CreateMicrosoftPlatformCryptoProvider(
                    CngAlgorithm.ECDsaP256,
                    creationOption: creationOption,
                    keySuffix: creationOption.ToString()));
        }

        private static void SignAndVerifyECDsa(CngKey key)
        {
            using (ECDsaCng ecdsa = new ECDsaCng(key))
            {
                byte[] data = { 12, 11, 02, 08, 25, 14, 11, 18, 16 };

                // using key directly
                byte[] signature = ecdsa.SignData(data, HashAlgorithmName.SHA256);
                VerifyTests(ecdsa, data, signature);

                // through cert
                CertificateRequest req = new CertificateRequest("CN=potato", ecdsa, HashAlgorithmName.SHA256);
                DateTimeOffset now = DateTimeOffset.UtcNow;
                using (X509Certificate2 cert = req.CreateSelfSigned(now, now.AddHours(1)))
                using (ECDsa certKey = cert.GetECDsaPrivateKey())
                using (ECDsa certPubKey = cert.GetECDsaPublicKey())
                {
                    Assert.NotNull(certKey);
                    Assert.NotNull(certPubKey);

                    VerifyTests(certPubKey, data, signature);
                    VerifyTests(certKey, data, signature);

                    Assert.ThrowsAny<CryptographicException>(() => certPubKey.SignData(data, HashAlgorithmName.SHA256));
                    signature = certKey.SignData(data, HashAlgorithmName.SHA256);

                    VerifyTests(ecdsa, data, signature);
                    VerifyTests(certPubKey, data, signature);
                    VerifyTests(certKey, data, signature);
                }

                // we can still sign/verify after disposing the cert
                signature = ecdsa.SignData(data, HashAlgorithmName.SHA256);
                VerifyTests(ecdsa, data, signature);
            }
        }

        private static void VerifyTests(ECDsa ecdsa, byte[] data, byte[] signature)
        {
            bool valid = ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            Assert.True(valid, "signature is not valid");

            signature[0] ^= 0xFF;
            valid = ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            Assert.False(valid, "tampered signature is valid");
            signature[0] ^= 0xFF;

            data[0] ^= 0xFF;
            valid = ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            Assert.False(valid, "tampered data is verified as valid");
            data[0] ^= 0xFF;

            // we call it second time and expect no issues with validation
            valid = ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
            Assert.True(valid, "signature is not valid");
        }
    }
}
