// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Rsa.Tests;
using Xunit;

namespace System.Security.Cryptography.Csp.Tests
{
    public class RSACryptoServiceProviderTests
    {
        const int PROV_RSA_FULL = 1;
        const int PROV_RSA_AES = 24;

        [Fact]
        public static void DefaultKeySize()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                Assert.Equal(1024, rsa.KeySize);
            }
        }

        [Fact]
        public static void PublicOnly_DefaultKey()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                // This will call the key into being, which should create a public/private pair,
                // therefore it should not be public-only.
                Assert.False(rsa.PublicOnly);
            }
        }

        [Fact]
        public static void PublicOnly_WithPrivateKey()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(TestData.RSA1024Params);

                Assert.False(rsa.PublicOnly);
            }
        }

        [Fact]
        public static void PublicOnly_WithNoPrivate()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                RSAParameters publicParams = new RSAParameters
                {
                    Modulus = TestData.RSA1024Params.Modulus,
                    Exponent = TestData.RSA1024Params.Exponent,
                };

                rsa.ImportParameters(publicParams);
                Assert.True(rsa.PublicOnly);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CspParameters on Unix
        public static void CreateKey_LegacyProvider()
        {
            CspParameters cspParameters = new CspParameters(PROV_RSA_FULL);

            using (var rsa = new RSACryptoServiceProvider(cspParameters))
            {
                CspKeyContainerInfo containerInfo = rsa.CspKeyContainerInfo;
                Assert.Equal(PROV_RSA_FULL, containerInfo.ProviderType);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CspParameters\CspKeyContainerInfo on Unix
        public static void CreateKey_LegacyProvider_RoundtripBlob()
        {
            const int KeySize = 512;

            CspParameters cspParameters = new CspParameters(PROV_RSA_FULL);
            byte[] blob;

            using (var rsa = new RSACryptoServiceProvider(KeySize, cspParameters))
            {
                CspKeyContainerInfo containerInfo = rsa.CspKeyContainerInfo;
                Assert.Equal(PROV_RSA_FULL, containerInfo.ProviderType);
                Assert.Equal(KeySize, rsa.KeySize);

                blob = rsa.ExportCspBlob(true);
            }

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportCspBlob(blob);

                CspKeyContainerInfo containerInfo = rsa.CspKeyContainerInfo;

                // The provider information is not persisted in the blob
                Assert.Equal(PROV_RSA_AES, containerInfo.ProviderType);
                Assert.Equal(KeySize, rsa.KeySize);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CspParameters\CspKeyContainerInfo on Unix
        public static void DefaultKey_Parameters()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                CspKeyContainerInfo keyContainerInfo = rsa.CspKeyContainerInfo;

                Assert.NotNull(keyContainerInfo);
                Assert.Equal(PROV_RSA_AES, keyContainerInfo.ProviderType);

                // This shouldn't be localized, so it should be safe to test on all cultures
                Assert.Equal("Microsoft Enhanced RSA and AES Cryptographic Provider", keyContainerInfo.ProviderName);

                Assert.Null(keyContainerInfo.KeyContainerName);
                Assert.Equal(string.Empty, keyContainerInfo.UniqueKeyContainerName);

                Assert.False(keyContainerInfo.HardwareDevice, "HardwareDevice");
                Assert.False(keyContainerInfo.MachineKeyStore, "MachineKeyStore");
                Assert.False(keyContainerInfo.Protected, "Protected");
                Assert.False(keyContainerInfo.Removable, "Removable");

                // Ephemeral keys don't successfully request the exportable bit.
                Assert.ThrowsAny<CryptographicException>(() => keyContainerInfo.Exportable);

                Assert.True(keyContainerInfo.RandomlyGenerated, "RandomlyGenerated");

                Assert.Equal(KeyNumber.Exchange, keyContainerInfo.KeyNumber);
            }
        }

        [Fact]
        public static void DefaultKey_NotPersisted()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                Assert.False(rsa.PersistKeyInCsp);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CspParameters on Unix
        public static void NamedKey_DefaultProvider()
        {
            const int KeySize = 2048;

            CspParameters cspParameters = new CspParameters
            {
                KeyContainerName = Guid.NewGuid().ToString(),
            };

            using (new RsaKeyLifetime(cspParameters))
            {
                byte[] privateBlob;
                string uniqueKeyContainerName;

                using (var rsa = new RSACryptoServiceProvider(KeySize, cspParameters))
                {
                    Assert.True(rsa.PersistKeyInCsp, "rsa.PersistKeyInCsp");
                    Assert.Equal(cspParameters.KeyContainerName, rsa.CspKeyContainerInfo.KeyContainerName);

                    uniqueKeyContainerName = rsa.CspKeyContainerInfo.UniqueKeyContainerName;
                    Assert.NotNull(uniqueKeyContainerName);
                    Assert.NotEqual(string.Empty, uniqueKeyContainerName);

                    privateBlob = rsa.ExportCspBlob(true);
                    Assert.True(rsa.CspKeyContainerInfo.Exportable, "rsa.CspKeyContainerInfo.Exportable");
                }

                // Fail if the key didn't persist
                cspParameters.Flags |= CspProviderFlags.UseExistingKey;

                using (var rsa = new RSACryptoServiceProvider(cspParameters))
                {
                    Assert.True(rsa.PersistKeyInCsp);
                    Assert.Equal(KeySize, rsa.KeySize);

                    Assert.Equal(uniqueKeyContainerName, rsa.CspKeyContainerInfo.UniqueKeyContainerName);

                    byte[] blob2 = rsa.ExportCspBlob(true);
                    Assert.Equal(privateBlob, blob2);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CspParameters on Unix
        public static void NamedKey_AlternateProvider()
        {
            const int KeySize = 512;

            CspParameters cspParameters = new CspParameters(PROV_RSA_FULL)
            {
                KeyContainerName = Guid.NewGuid().ToString(),
            };

            using (new RsaKeyLifetime(cspParameters))
            {
                byte[] privateBlob;
                string uniqueKeyContainerName;

                using (var rsa = new RSACryptoServiceProvider(KeySize, cspParameters))
                {
                    Assert.True(rsa.PersistKeyInCsp);
                    Assert.Equal(PROV_RSA_FULL, rsa.CspKeyContainerInfo.ProviderType);

                    privateBlob = rsa.ExportCspBlob(true);

                    Assert.Equal(cspParameters.KeyContainerName, rsa.CspKeyContainerInfo.KeyContainerName);

                    uniqueKeyContainerName = rsa.CspKeyContainerInfo.UniqueKeyContainerName;
                    Assert.NotNull(uniqueKeyContainerName);
                    Assert.NotEqual(string.Empty, uniqueKeyContainerName);
                }

                // Fail if the key didn't persist
                cspParameters.Flags |= CspProviderFlags.UseExistingKey;

                using (var rsa = new RSACryptoServiceProvider(cspParameters))
                {
                    Assert.True(rsa.PersistKeyInCsp);
                    Assert.Equal(KeySize, rsa.KeySize);

                    // Since we're specifying the provider explicitly it should still match.
                    Assert.Equal(PROV_RSA_FULL, rsa.CspKeyContainerInfo.ProviderType);

                    Assert.Equal(uniqueKeyContainerName, rsa.CspKeyContainerInfo.UniqueKeyContainerName);

                    byte[] blob2 = rsa.ExportCspBlob(true);
                    Assert.Equal(privateBlob, blob2);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CspParameters on Unix
        public static void NonExportable_Ephemeral()
        {
            CspParameters cspParameters = new CspParameters
            {
                Flags = CspProviderFlags.UseNonExportableKey,
            };

            using (var rsa = new RSACryptoServiceProvider(cspParameters))
            {
                // Ephemeral keys don't successfully request the exportable bit.
                Assert.ThrowsAny<CryptographicException>(() => rsa.CspKeyContainerInfo.Exportable);

                Assert.ThrowsAny<CryptographicException>(() => rsa.ExportCspBlob(true));
                Assert.ThrowsAny<CryptographicException>(() => rsa.ExportParameters(true));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // No support for CspParameters on Unix
        public static void NonExportable_Persisted()
        {
            CspParameters cspParameters = new CspParameters
            {
                KeyContainerName = Guid.NewGuid().ToString(),
                Flags = CspProviderFlags.UseNonExportableKey,
            };

            using (new RsaKeyLifetime(cspParameters))
            {
                using (var rsa = new RSACryptoServiceProvider(cspParameters))
                {
                    Assert.False(rsa.CspKeyContainerInfo.Exportable, "rsa.CspKeyContainerInfo.Exportable");

                    Assert.ThrowsAny<CryptographicException>(() => rsa.ExportCspBlob(true));
                    Assert.ThrowsAny<CryptographicException>(() => rsa.ExportParameters(true));
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void Ctor_UseCspParameter_Throws_Unix()
        {
            var cspParameters = new CspParameters();
            Assert.Throws<PlatformNotSupportedException>(() => new RSACryptoServiceProvider(cspParameters));
            Assert.Throws<PlatformNotSupportedException>(() => new RSACryptoServiceProvider(0, cspParameters));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void CspKeyContainerInfo_Throws_Unix()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                Assert.Throws<PlatformNotSupportedException>(() => (rsa.CspKeyContainerInfo));
            }
        }

        [Fact]
        public static void ImportParameters_ExponentTooBig_Throws()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                // Verify that Unix shims and Windows Csp both throws the same exception when large Exponent imported
                Assert.ThrowsAny<CryptographicException>(() => rsa.ImportParameters(TestData.RsaBigExponentParams));
            }
        }

        [Fact]
        public static void SignHash_DefaultAlgorithm_Success()
        {
            byte[] hashVal = SHA1.HashData(TestData.HelloBytes);

            using (var rsa = new RSACryptoServiceProvider())
            {
                byte[] signVal = rsa.SignHash(hashVal, null);
                Assert.True(rsa.VerifyHash(hashVal, null, signVal));
            }
        }

        [Fact]
        public static void VerifyHash_DefaultAlgorithm_Success()
        {
            byte[] hashVal = SHA1.HashData(TestData.HelloBytes);

            using (var rsa = new RSACryptoServiceProvider())
            {
                byte[] signVal = rsa.SignData(TestData.HelloBytes, "SHA1");
                Assert.True(rsa.VerifyHash(hashVal, null, signVal));
            }
        }

        [Fact]
        public static void Encrypt_InvalidPaddingMode_Throws()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                Assert.Throws<CryptographicException>(() => rsa.Encrypt(TestData.HelloBytes, RSAEncryptionPadding.OaepSHA256));
            }
        }

        [Fact]
        public static void Decrypt_InvalidPaddingMode_Throws()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                Assert.Throws<CryptographicException>(() => rsa.Decrypt(TestData.HelloBytes, RSAEncryptionPadding.OaepSHA256));
            }
        }

        [Fact]
        public static void Sign_InvalidPaddingMode_Throws()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                Assert.Throws<CryptographicException>(() => rsa.SignData(TestData.HelloBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pss));
            }
        }

        [Fact]
        public static void Verify_InvalidPaddingMode_Throws()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                byte[] sig = rsa.SignData(TestData.HelloBytes, "SHA1");
                Assert.Throws<CryptographicException>(() => rsa.VerifyData(TestData.HelloBytes, sig, HashAlgorithmName.SHA1, RSASignaturePadding.Pss));
            }
        }

        [Fact]
        public static void SignatureAlgorithm_Success()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                Assert.Equal("http://www.w3.org/2000/09/xmldsig#rsa-sha1", rsa.SignatureAlgorithm);
            }
        }

        [Fact]
        public static void SignData_VerifyHash_CaseInsensitive_Success()
        {
            byte[] hashVal = SHA1.HashData(TestData.HelloBytes);

            using (var rsa = new RSACryptoServiceProvider())
            {
                byte[] signVal = rsa.SignData(TestData.HelloBytes, "SHA1");
                Assert.True(rsa.VerifyHash(hashVal, "SHA1", signVal));

                signVal = rsa.SignData(TestData.HelloBytes, "sha1");
                Assert.True(rsa.VerifyHash(hashVal, "sha1", signVal));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Only Unix has _impl shim pattern
        public static void TestShimOverloads_Unix()
        {
            ShimHelpers.VerifyAllBaseMembersOverloaded(typeof(RSACryptoServiceProvider));
        }

        private sealed class RsaKeyLifetime : IDisposable
        {
            private readonly CspParameters _cspParameters;

            internal RsaKeyLifetime(CspParameters cspParameters)
            {
                const CspProviderFlags CopyableFlags =
                    CspProviderFlags.UseMachineKeyStore;

                _cspParameters = new CspParameters(
                    cspParameters.ProviderType,
                    cspParameters.ProviderName,
                    cspParameters.KeyContainerName)
                {
                    // If the test failed before creating the key, don't bother recreating it.
                    Flags = (cspParameters.Flags & CopyableFlags) | CspProviderFlags.UseExistingKey,
                };
            }

            public void Dispose()
            {
                try
                {
                    using (var rsa = new RSACryptoServiceProvider(_cspParameters))
                    {
                        // Delete the key at the end of this using
                        rsa.PersistKeyInCsp = false;
                    }
                }
                catch (CryptographicException)
                {
                }
            }
        }
    }
}
